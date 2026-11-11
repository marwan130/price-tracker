from __future__ import annotations

import re
from datetime import timedelta
from pathlib import Path
from unittest import TestCase

from aio_pika.message import encode_expiration
from price_tracker_scraper.extraction import (
    Extraction,
    _extract_page,
    _validate_url,
    as_payload,
)
from price_tracker_scraper.worker import _is_retryable_failure
from scrapling import Selector


class _JsonLdSelection:
    def __init__(self, values: list[str]):
        self._values = values

    def getall(self) -> list[str]:
        return self._values

    def get(self) -> str | None:
        return self._values[0] if self._values else None


class _FixturePage:
    status = 200

    def __init__(self, html: str):
        self.html = html

    def css(self, selector: str) -> _JsonLdSelection:
        if selector != 'script[type="application/ld+json"]::text':
            return _JsonLdSelection([])
        return _JsonLdSelection(re.findall(
            r'<script type="application/ld\+json">(.*?)</script>',
            self.html,
            flags=re.DOTALL,
        ))


class _StatusPage:
    status = 200

    def __init__(self, html: str, url: str):
        self.selector = Selector(html, url=url)

    def css(self, query: str):
        return self.selector.css(query)


class ExtractionFixtureTests(TestCase):
    def test_extracts_json_ld_product_fields_without_currency_inference(self) -> None:
        fixture = Path(__file__).parent / "fixtures" / "product-jsonld.html"
        page = _FixturePage(fixture.read_text(encoding="utf-8"))
        result = _extract_page(page)

        self.assertEqual(result.title, "Example Phone 128 GB")
        self.assertEqual(result.price, "24999.00")
        self.assertEqual(result.currency, "EGP")
        self.assertEqual(result.availability, "InStock")
        self.assertEqual(result.extraction_method, "json-ld")

    def test_prefers_the_most_complete_product_and_valid_offer(self) -> None:
        page = _FixturePage('''
          <script type="application/ld+json">[
            {"@type":"Product","name":"Unpriced recommendation"},
            {"@type":"Product","name":"Actual phone","offers":[
              {"price":"Call for price","priceCurrency":"EGP"},
              {"price":"12000.50","priceCurrency":"egp","availability":"https://schema.org/InStock"}
            ]}
          ]</script>
        ''')
        result = _extract_page(page)

        self.assertEqual(result.title, "Actual phone")
        self.assertEqual(result.price, "12000.50")
        self.assertEqual(result.currency, "EGP")
        self.assertEqual(result.availability, "InStock")

    def test_resolves_relative_product_images_against_the_product_page(self) -> None:
        page = _FixturePage('''
          <script type="application/ld+json">{"@type":"Product","name":"Phone","image":"/media/phone.webp","offers":{"price":"100","priceCurrency":"EGP"}}</script>
        ''')

        result = _extract_page(page, "https://shop.example/products/phone")

        self.assertEqual(result.image_url, "https://shop.example/media/phone.webp")

    def test_uses_open_graph_image_when_product_json_ld_omits_image(self) -> None:
        page = _StatusPage('''
          <html><head><meta property="og:image" content="//cdn.example/phone.webp"></head><body>
          <script type="application/ld+json">{"@type":"Product","name":"Phone","offers":{"price":"100","priceCurrency":"AED"}}</script>
          </body></html>
        ''', "https://www.noon.com/uae-en/phone/N123/p/")

        result = _extract_page(page, "https://www.noon.com/uae-en/phone/N123/p/")

        self.assertEqual(result.image_url, "https://cdn.example/phone.webp")

    def test_drops_store_chrome_og_images_before_they_render_as_the_product(self) -> None:
        # Storefronts ship promo og:image variants (banners, share icons) that the
        # og/page-image scan matches ahead of the real photo in document order.
        page = _StatusPage('''
          <html><head>
          <meta property="og:image" content="https://www.noon.com/static/version/img/banner.jpg">
          <meta property="og:image" content="https://www.noon.com/img/social/whatsapp.svg">
          </head><body>
          <script type="application/ld+json">{"@type":"Product","name":"Phone","offers":{"price":"100","priceCurrency":"AED"}}</script>
          </body></html>
        ''', "https://www.noon.com/uae-en/phone/N123/p/")

        result = _extract_page(page, "https://www.noon.com/uae-en/phone/N123/p/")

        self.assertIsNone(result.image_url)

    def test_fills_missing_json_ld_price_from_explicit_store_metadata(self) -> None:
        url = "https://www.noon.com/uae-en/phone/N123/p/"
        page = _StatusPage('''
          <html><head>
          <meta property="product:price:amount" content="249.50">
          <meta property="product:price:currency" content="AED">
          </head><body>
          <script type="application/ld+json">{"@type":"Product","name":"Phone"}</script>
          </body></html>
        ''', url)

        result = _extract_page(page, url)

        self.assertEqual(result.title, "Phone")
        self.assertEqual(result.price, "249.50")
        self.assertEqual(result.currency, "AED")
        self.assertEqual(result.extraction_method, "json-ld+noon-selectors")

    def test_extracts_labeled_amazon_price_and_primary_product_image(self) -> None:
        url = "https://www.amazon.ae/dp/B0TEST1234"
        page = _StatusPage('''
          <html><body>
          <span id="productTitle">Samsung Galaxy A16 4GB RAM 128GB</span>
          <div id="corePrice_feature_div"><span class="a-offscreen">AED570.00</span></div>
          <img id="landingImage" src="https://images.example/medium.jpg" data-old-hires="https://images.example/full.jpg">
          </body></html>
        ''', url)

        result = _extract_page(page, url)

        self.assertEqual(result.title, "Samsung Galaxy A16 4GB RAM 128GB")
        self.assertEqual(result.price, "570.00")
        self.assertEqual(result.currency, "AED")
        self.assertEqual(result.image_url, "https://images.example/full.jpg")

    def test_extracts_amazon_offscreen_price_when_legacy_price_block_is_absent(self) -> None:
        url = "https://www.amazon.sa/dp/B0TEST1234"
        page = _StatusPage('''
          <html><body>
          <span id="productTitle">Samsung Galaxy S25 256GB</span>
          <div id="corePriceDisplay_desktop_feature_div">
            <span class="a-price"><span class="a-offscreen">SAR2,299.00</span></span>
            <span class="a-price a-text-price"><span class="a-offscreen">SAR2,699.00</span></span>
          </div>
          </body></html>
        ''', url)

        result = _extract_page(page, url)

        self.assertEqual(result.title, "Samsung Galaxy S25 256GB")
        self.assertEqual(result.price, "2299.00")
        self.assertEqual(result.currency, "SAR")

    def test_serializes_extracted_image_with_api_contract_field_name(self) -> None:
        payload = as_payload(Extraction("Phone", "100", "AED", "https://cdn.example/phone.webp", "InStock", "json-ld"))

        self.assertEqual(payload["imageUrl"], "https://cdn.example/phone.webp")
        self.assertNotIn("image_url", payload)

    def test_store_specific_fixtures_extract_labeled_prices_without_currency_guessing(self) -> None:
        fixtures = Path(__file__).parent / "fixtures" / "stores"
        cases = (
            ("jumia-eg.html", "https://www.jumia.com.eg/example"),
            ("noon-eg.html", "https://www.noon.com/egypt-en/example"),
            ("amazon-eg.html", "https://www.amazon.eg/dp/example"),
            ("namshi-eg.html", "https://en.namshi.com/eg/example.html"),
        )
        for filename, url in cases:
            with self.subTest(store=filename):
                page = _StatusPage((fixtures / filename).read_text(encoding="utf-8"), url)
                result = _extract_page(page, url)
                self.assertEqual(result.title, "Fixture Product")
                self.assertEqual(result.price, "1234.50")
                self.assertEqual(result.currency, "EGP")
                self.assertIsNotNone(result.extraction_method)

    def test_amazon_out_of_stock_page_streams_the_unavailable_state_without_a_price(self) -> None:
        # Amazon's OOS page drops the JSON-LD block and the buybox price slab
        # together; the extraction must still carry the out-of-stock state so
        # the card shows the reason instead of an unexplained missing price.
        url = "https://www.amazon.ae/dp/B0TEST1234"
        page = _StatusPage(
            (Path(__file__).parent / "fixtures" / "stores" / "amazon-unavailable.html").read_text(encoding="utf-8"),
            url,
        )

        result = _extract_page(page, url)

        self.assertEqual(result.title, "Fixture Product")
        self.assertIsNone(result.price)
        self.assertEqual(result.availability, "OutOfStock")
        self.assertEqual(result.image_url, "https://m.media-amazon.com/images/I/fixture.jpg")
        self.assertEqual(result.extraction_method, "amazon-selectors")

    def test_amazon_multi_size_image_json_picks_the_largest_declared_render(self) -> None:
        # The src beside data-a-dynamic-image is a placeholder crop; the JSON
        # map holds every render of the hero, and the largest area must win.
        url = "https://www.amazon.ae/dp/B0DYNAMIC1"
        page = _StatusPage(
            '''
            <html><body><h1 id="productTitle">Fixture Product</h1>
            <img id="landingImage" src="https://m.media-amazon.com/images/I/small._SX38_SY50_.jpg"
              data-a-dynamic-image='{"https://m.media-amazon.com/images/I/small._SX38_SY50_.jpg":[38,50],"https://m.media-amazon.com/images/I/hero._SX330_SY330_.jpg":[330,330]}'
              src2="ignored"></body></html>
            ''',
            url,
        )

        result = _extract_page(page, url)

        self.assertEqual(result.image_url, "https://m.media-amazon.com/images/I/hero._SX330_SY330_.jpg")

    def test_json_ld_image_array_prefers_the_canonical_hero_over_the_first_entry(self) -> None:
        # Marketplace JSON-LD lists thumbnail variants ("US40" recommendation
        # rows) before the SY300/SX300 hero; the first entry must not win.
        url = "https://www.amazon.eg/dp/B0IMAGE1"
        page = _StatusPage(
            '''
            <html><body>
            <script type="application/ld+json">{"@type":"Product","name":"Fixture Headphones",
              "image":["https://m.media-amazon.com/images/I/tiny._US40_.jpg","https://m.media-amazon.com/images/I/hero._SY300_SX300_.jpg"],
              "offers":{"price":"3300","priceCurrency":"EGP"}}</script>
            </body></html>
            ''',
            url,
        )

        result = _extract_page(page, url)

        self.assertEqual(result.image_url, "https://m.media-amazon.com/images/I/hero._SY300_SX300_.jpg")

    def test_shopify_product_group_has_variant_members_are_extracted_as_products(self) -> None:
        # Shopify themes (and any schema.org draft consumer) publish a
        # ProductGroup whose sellable products hang off hasVariant; the group's
        # own block carries no offer, so the variant member must win.
        url = "https://smartkoshk.com/products/fixture-oneplus-16"
        page = _StatusPage(
            '''
            <html><body><script type="application/ld+json">{
              "@context":"https://schema.org/","@id":"/products/fixture-oneplus-16#product","@type":"ProductGroup",
              "name":"Fixture OnePlus 16","hasVariant":[
                {"@type":"Product","name":"Fixture OnePlus 16 - Silver / 256GB","image":["https://smartkoshk.com/cdn/shop/files/oneplus-silver.png"],
                 "offers":{"@type":"Offer","availability":"https://schema.org/InStock","price":"54990.00","priceCurrency":"EGP"}},
                {"@type":"Product","name":"Fixture OnePlus 16 - Black / 1TB","image":"https://smartkoshk.com/cdn/shop/files/oneplus-black.png",
                 "offers":{"@type":"Offer","availability":"https://schema.org/OutOfStock","price":"71000.00","priceCurrency":"EGP"}}]}
            </script></body></html>
            ''',
            url,
        )

        result = _extract_page(page, url)

        self.assertEqual(result.price, "54990.00")
        self.assertEqual(result.currency, "EGP")
        self.assertEqual(result.title, "Fixture OnePlus 16 - Silver / 256GB")
        self.assertEqual(result.image_url, "https://smartkoshk.com/cdn/shop/files/oneplus-silver.png")
        self.assertEqual(result.availability, "InStock")
        self.assertEqual(result.extraction_method, "json-ld")

    def test_unknown_store_pages_extract_through_generic_meta_tags(self) -> None:
        # Long-tail stores have no hand-written selectors; the meta-tag and
        # itemprop fallbacks must still surface title, price and currency so a
        # pasted link imports instead of failing.
        url = "https://smarttailor.example.eg/products/fixture-gown"
        page = _StatusPage(
            '''
            <html><head>
              <meta property="og:title" content="Silk Evening Gown (Midnight)">
              <meta property="product:price:amount" content="4250">
              <meta property="product:price:currency" content="EGP">
            </head><body><h1>Silk Evening Gown (Midnight)</h1></body></html>
            ''',
            url,
        )

        result = _extract_page(page, url)

        self.assertEqual(result.title, "Silk Evening Gown (Midnight)")
        self.assertEqual(result.price, "4250")
        self.assertEqual(result.currency, "EGP")
        self.assertEqual(result.extraction_method, "generic-selectors")

    def test_http_og_image_is_promoted_to_https(self) -> None:
        # Shopify still writes og:image with a bare http scheme although the
        # same path serves over https; the photo must survive, not be dropped.
        url = "https://smartkoshk.com/products/fixture-http-image"
        page = _StatusPage(
            '''
            <html><head><meta property="og:image" content="http://smartkoshk.com/cdn/shop/files/hero.png"></head>
            <body></body></html>
            ''',
            url,
        )

        from price_tracker_scraper.extraction import _extract_page_image
        promoted = _extract_page_image(page, url)

        self.assertEqual(promoted, "https://smartkoshk.com/cdn/shop/files/hero.png")

    def test_store_meta_availability_maps_to_the_schema_org_vocabulary(self) -> None:
        url = "https://www.jumia.com.eg/example-12345.html"
        page = _StatusPage('''
          <html><head><title>Fixture Product</title></head><body>
          <meta itemprop="priceCurrency" content="EGP">
          <link itemprop="availability" href="https://schema.org/OutOfStock">
          </body></html>
        ''', url)

        result = _extract_page(page, url)

        self.assertIsNone(result.price)
        self.assertEqual(result.availability, "OutOfStock")

    def test_unknown_availability_text_stays_unknown_instead_of_guessing(self) -> None:
        url = "https://www.noon.com/uae-en/phone/N123/p/"
        page = _StatusPage('''
          <html><head><meta property="og:availability" content="MadeToOrder"></head></html>
        ''', url)

        result = _extract_page(page, url)

        self.assertIsNone(result.availability)

    def test_unavailable_slab_never_overrides_a_still_priced_product(self) -> None:
        # Amazon.eg renders the same "Currently unavailable" shell under
        # variant offers, so slab text loses against any extracted price.
        url = "https://www.amazon.ae/dp/B0TEST1234"
        page = _StatusPage('''
          <html><body>
          <span id="productTitle">Phone 128GB</span>
          <div id="corePrice_feature_div"><span class="a-offscreen">AED570.00</span></div>
          <div id="availability"><span>Currently unavailable.</span></div>
          </body></html>
        ''', url)

        result = _extract_page(page, url)

        self.assertEqual(result.price, "570.00")
        self.assertIsNone(result.availability)

    def test_rejects_non_public_literal_host_before_fetch(self) -> None:
        with self.assertRaisesRegex(ValueError, "non_public_host"):
            _validate_url("https://127.0.0.1/private")

    def test_retry_expiration_accepts_timedelta_and_preserves_milliseconds(self) -> None:
        self.assertEqual(encode_expiration(timedelta(milliseconds=5_852)), "5852")

    def test_retries_transient_statuses_but_not_permanent_client_errors(self) -> None:
        self.assertFalse(_is_retryable_failure("http_status:403"))
        self.assertFalse(_is_retryable_failure("http_status:404"))
        self.assertTrue(_is_retryable_failure("http_status:429"))
        self.assertTrue(_is_retryable_failure("http_status:503"))
        self.assertTrue(_is_retryable_failure("fetch_timeout"))

