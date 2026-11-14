export type Locale = 'en' | 'ar';

export interface Strings {
  requestFailed: string;
  navDashboard: string;
  navDiscover: string;
  navProducts: string;
  navCompare: string;
  navNotifications: string;
  navSettings: string;
  sideWorkspace: string;
  sideMarket: string;
  sideChooseMarket: string;
  sideCollapse: string;
  sideExpand: string;
  breadcrumbProductDetails: string;
  globalSearchPlaceholder: string;
  globalSearchLabel: string;
  searchVerifying: string;
  clearSearch: string;
  searchOpenLinkFailed: string;
  searchOpeningLink: string;
  searchLinkRequired: string;
  navigationLabel: string;
  notificationsLabel: string;
  notificationsUnread: (n: number) => string;
  notificationsDeliveryLabel: string;
  notificationUpdateFailed: string;
  notificationReadFailed: string;
  notificationsDeliveryState: (status: string) => string;
  operationsQueueMessages: (count: number) => string;
  operationsQueueConsumers: (count: number) => string;
  signIn: string;
  signOut: string;
  signedOut: string;
  openNavigation: string;
  closeNavigation: string;
  marketLabel: string;
  marketAll: string;
  marketChoosePrompt: string;
  marketEG: string;
  marketSA: string;
  marketAE: string;
  chooseMarket: string;
  dashboardEyebrow: string;
  dashboardHeading: string;
  dashboardSubheading: string;
  dashboardExplore: string;
  dashboardAddHeading: string;
  dashboardAddBody: string;
  dashboardAddPlaceholder: string;
  dashboardFindButton: string;
  dashboardTrackingHeading: string;
  dashboardTrackingSubheading: string;
  dashboardViewAll: string;
  dashboardTrackedMetric: string;
  dashboardTrackedNote: string;
  dashboardAtTargetMetric: string;
  dashboardAtTargetNote: string;
  dashboardNearTargetMetric: string;
  dashboardNearTargetNote: string;
  dashboardUnreadMetric: string;
  dashboardUnreadNote: string;
  dashboardRecentHeading: string;
  dashboardRecentSubheading: string;
  dashboardLoadError: string;
  dashboardGuestEyebrow: string;
  dashboardGuestHeading: string;
  dashboardGuestBody: string;
  dashboardGuestCta: string;
  dashboardDataNote: string;
  dashboardNothingTracked: string;
  dashboardNothingTrackedBody: string;
  dashboardTryAgain: string;
  dashboardTrackingLoadError: string;
  dropsEyebrow: string;
  dropsHeading: string;
  dropsSubheading: string;
  dropsDiscoverMore: string;
  dropsViewHistory: string;
  dropsLower: (pct: string) => string;
  discoverEyebrow: string;
  discoverHeading: string;
  discoverSubheading: string;
  discoverSearchPlaceholder: string;
  discoverSearchLabel: string;
  discoverSearchButton: string;
  discoverSearching: string;
  discoverResultsCount: (n: number) => string;
  discoverResultsLive: (n: number) => string;
  discoverVerifiedPrices: (n: number) => string;
  discoverPriceFilter: string;
  discoverMinPrice: string;
  discoverMaxPrice: string;
  discoverAnyPrice: string;
  discoverSortLabel: string;
  discoverSortRelevance: string;
  discoverSortPrice: string;
  discoverSortPriceAsc: string;
  discoverSortPriceDesc: string;
  discoverSortVerified: string;
  discoverSortStore: string;
  discoverSortName: string;
  discoverFilterMarket: string;
  discoverFilterAll: string;
  discoverNote: string;
  discoverVerified: string;
  discoverUnverified: string;
  discoverPreviewTrack: string;
  discoverAddCompare: string;
  discoverRemoveCompare: string;
  discoverInspectStore: string;
  discoverObservedPrice: string;
  discoverPriceUnavailable: string;
  discoverVerifyAtStore: string;
  discoverCurrencyUnavailable: string;
  discoverDiscoverySource: string;
  discoverNoResults: string;
  discoverNoResultsBody: string;
  discoverNoResultsMarket: string;
  discoverTryAgain: string;
  discoverSomeFailed: string;
  discoverTemporarilyUnavailable: string;
  discoverTimedOut: string;
  discoverListView: string;
  discoverGridView: string;
  discoverProvidersProgress: (n: number) => string;
  discoverSearchResultsEyebrow: string;
  discoverBestDealsEyebrow: string;
  discoverSearchResultsHeading: (query: string) => string;
  discoverBestDealsHeading: string;
  discoverBack: string;
  discoverSearchRetryMessage: string;
  discoverTopPriceDrops: string;
  discoverClearPrice: string;
  discoverPriceRangeError: string;
  discoverStoreCurrencyLabel: string;
  discoverSearchingStores: string;
  discoverStillSearching: string;
  discoverSearchingResults: (found: number, verified: number) => string;
  discoverInStock: string;
  discoverOutOfStock: string;
  discoverTrackPrice: string;
  discoverPricePending: string;
  discoverPriceUnverified: string;
  discoverCompared: string;
  discoverCompare: string;
  discoverStore: string;
  discoverSave: string;
  discoverViewDeal: string;
  discoverNoRecentDrops: string;
  discoverNoRecentDropsBody: string;
  dealCategoryElectronics: string;
  dealCategoryMobiles: string;
  dealCategoryComputers: string;
  dealCategoryHome: string;
  dealCategoryGaming: string;
  dealCategoryAudio: string;
  dealCategoryFashion: string;
  dealCategoryBeauty: string;
  allCategories: string;
  searchCategoryMobilePhones: string;
  searchCategoryLaptops: string;
  searchCategoryTablets: string;
  searchCategoryTvsMonitors: string;
  searchCategoryWearables: string;
  searchCategoryHomeAppliances: string;
  searchCategoryFurniture: string;
  searchCategoryGeneral: string;
  overviewTrendingEyebrow: string;
  overviewTrendingHeading: string;
  overviewTrendingBody: (days: number) => string;
  overviewCategoriesEyebrow: string;
  overviewCategoriesHeading: string;
  overviewCategoriesBody: (days: number) => string;
  overviewProductsEyebrow: string;
  overviewProductsHeading: string;
  overviewProductsBody: (days: number) => string;
  overviewStoresEyebrow: string;
  overviewStoresHeading: string;
  overviewStoresBody: (days: number) => string;
  overviewNewTracking: (n: number) => string;
  overviewVerifiedOffer: (n: number) => string;
  overviewEmptyHeading: string;
  overviewEmptyBody: string;
  overviewHighlightsUnavailable: string;
  myProductsEyebrow: string;
  myProductsHeading: string;
  myProductsAddProduct: string;
  myProductsSearchPlaceholder: string;
  myProductsSearchLabel: string;
  myProductsStatusFilter: string;
  myProductsAllStatuses: string;
  myProductsCategoryFilter: string;
  myProductsAllCategories: string;
  myProductsMarketFilter: string;
  myProductsAllMarkets: string;
  myProductsPriceFilter: string;
  myProductsSortBy: string;
  myProductsFiltering: string;
  myProductsSortProductName: string;
  myProductsSortLowestPrice: string;
  myProductsSortTargetPrice: string;
  myProductsSortPriceChange: string;
  myProductsSortBiggestDrop: string;
  myProductsSortTargetProximity: string;
  myProductsSortLastUpdated: string;
  myProductsSortTrackingStatus: string;
  myProductsAnyPrice: string;
  myProductsAtOrBelowTarget: string;
  myProductsNearTarget: string;
  myProductsAboveTarget: string;
  myProductsPriceUnavailable: string;
  myProductsShowPaused: string;
  myProductsTable: string;
  myProductsCards: string;
  myProductsViewLabel: string;
  myProductsSelected: (n: number) => string;
  myProductsResume: string;
  myProductsPause: string;
  myProductsRemoveSelected: string;
  myProductsNoMatch: string;
  myProductsNoMatchBody: string;
  myProductsEmpty: string;
  myProductsEmptyBody: string;
  myProductsLoadError: string;
  myProductsDataNote: string;
  myProductsViewProduct: string;
  myProductsSelectLabel: (name: string) => string;
  myProductsSelectAll: string;
  myProductsBulkFailed: (n: number) => string;
  myProductsBulkRemovedMsg: string;
  myProductsBulkUpdatedMsg: string;
  colProduct: string;
  colStore: string;
  colCurrentPrice: string;
  colTargetPrice: string;
  colDifference: string;
  colStatus: string;
  colActions: string;
  sortBy: (label: string) => string;
  atTarget: string;
  statusTracking: string;
  statusPaused: string;
  pauseTracking: string;
  resumeTracking: string;
  removeTracking: string;
  removeTrackingConfirm: string;
  trackingRemoved: string;
  trackingResumed: string;
  trackingPaused: string;
  trackingPreferencesHeading: string;
  trackingSavePreferences: string;
  trackingEmailTarget: string;
  trackingEmailFurther: string;
  trackingPreferencesSaved: string;
  actionFailed: string;
  compareEyebrow: string;
  compareHeading: string;
  compareLoading: string;
  compareSubheading: string;
  compareEmpty: string;
  compareEmptyBody: string;
  compareRemove: string;
  compareViewProduct: string;
  compareLatestPrice: string;
  compareTargetPrice: string;
  compareOfferCount: (n: number) => string;
  compareOffers: string;
  compareBestStore: string;
  compareMarket: string;
  compareNotTracked: string;
  compareHistoryHeading: string;
  compareHistoryIntro: string;
  compareHistoryPeriod: string;
  compareHistoryPeriod30: string;
  compareHistoryPeriod90: string;
  compareHistoryPeriod365: string;
  compareHistoryPeriodAll: string;
  compareHistoryStores: string;
  compareHistorySampled: string;
  compareHistoryRange: (from: string, to: string, currency: string) => string;
  compareHistoryUnavailable: (currency: string) => string;
  compareNoHistoryForCurrency: (currency: string) => string;
  compareProduct: string;
  compareBrand: string;
  compareCategory: string;
  compareUnavailable: string;
  compareUnavailableNote: string;
  compareBackToProducts: string;
  compareError: string;
  storeOffersHeading: string;
  dealQualityHeading: string;
  dealQualityInsufficient: string;
  dealQualityScoreLabel: (score: number) => string;
  dealQualityTooltip: string;
  productDetailsUnavailable: string;
  productDetailsLoading: string;
  productOffersCount: (n: number) => string;
  productMarketCurrency: string;
  productLastChecked: string;
  productMonitoring: string;
  productActiveListing: string;
  productInactiveListing: string;
  productIncludeOffer: string;
  productExcludeOffer: string;
  productOfferIncluded: (store: string) => string;
  productOfferExcluded: (store: string) => string;
  productExcluded: string;
  productIncluded: string;
  productNoStoredOffers: string;
  productNoStoredOffersBody: string;
  productHistoryPeriod7: string;
  productHistoryByStore: string;
  productHistoryPartial: string;
  productHistorySampled: string;
  productHistoryChartLabel: (from: string, to: string, min: string, max: string) => string;
  productTrackingActivity: string;
  productTrackingStarted: string;
  productNavOffers: string;
  productNavHistory: string;
  productNavActivity: string;
  productActivityEmpty: string;
  productLowestVerified: string;
  productHistoryEmpty: string;
  productHistoryEmptyBody: string;
  productHistoryLoadFailed: string;
  dashFeedEyebrow: string;
  dashFeedHeading: string;
  dashFeedWas: string;
  dashFeedMovementDown: (percent: number, date: string) => string;
  dashFeedMovementUp: (percent: number, date: string) => string;
  dashFeedStores: (n: number) => string;
  dashFeedLoading: string;
  dashFeedMore: string;
  dashFeedEnd: string;
  dashFeedReshuffle: string;
  dashFeedError: string;
  dashFeedEmpty: string;
  dashFeedEmptyBody: string;
  dashFeedOpen: (name: string) => string;
  productMovementDown: (percent: number, date: string) => string;
  productMovementUp: (percent: number, date: string) => string;
  productOfferBest: string;
  productOfferVerified: string;
  productOfferStale: string;
  productOfferUnverified: string;
  productOfferNoPrice: string;
  productOfferUnverifiedHint: string;
  productOfferVerifiedHint: string;
  productOfferStaleHint: string;
  productOfferNoPriceHint: string;
  productPriceNotConfirmed: string;
  productPriceSource: (store: string) => string;
  productImageMissing: string;
  notFoundAriaLabel: string;
  notFoundStamp: string;
  notFoundEyebrow: string;
  notFoundHeading: string;
  notFoundDescription: string;
  notFoundObservation: string;
  notFoundStatus: string;
  notFoundStatusValue: string;
  notFoundRecommendation: string;
  notFoundExploreDeals: string;
  notFoundDashboard: string;
  skipToContent: string;
  notificationsEyebrow: string;
  notificationsHeading: string;
  notificationsEmpty: string;
  notificationsEmptyBody: string;
  notificationsMarkRead: string;
  notificationsMarkAllRead: string;
  notificationsFilterAll: string;
  notificationsFilterUnread: string;
  notificationsFilterRead: string;
  notificationsShowFilter: string;
  notificationsFilterType: string;
  notificationsFilterTarget: string;
  notificationsFilterFurther: string;
  notificationsFilterStopped: string;
  notificationsTargetReached: string;
  notificationsFurtherDrop: string;
  notificationsTrackingStopped: string;
  notificationsAt: (price: string, target: string) => string;
  notificationsFrom: (store: string) => string;
  notificationsEquivalent: string;
  notificationsLoadError: string;
  notificationsLoadErrorBody: string;
  settingsEyebrow: string;
  settingsHeading: string;
  settingsSubheading: string;
  settingsAccountHeading: string;
  settingsAccountGuest: string;
  guestPageAccessBody: string;
  guestPageAccessHeading: string;
  settingsAccountAccess: string;
  settingsAppearanceHeading: string;
  settingsAppearanceSubheading: string;
  settingsTheme: string;
  settingsThemeLight: string;
  settingsThemeDark: string;
  settingsThemeSystem: string;
  settingsLanguage: string;
  settingsLanguageEn: string;
  settingsLanguageAr: string;
  settingsMarketsHeading: string;
  settingsMarketsSubheading: string;
  settingsPreferredMarket: string;
  settingsDisplayCurrency: string;
  settingsCurrencyMarket: string;
  settingsChooseMarket: string;
  settingsMarketEgypt: string;
  settingsMarketSaudi: string;
  settingsMarketUAE: string;
  settingsNotificationsHeading: string;
  settingsNotificationsSubheading: string;
  settingsNotificationsPerProduct: string;
  settingsTrackingHeading: string;
  settingsTrackingSubheading: string;
  settingsTrackingPaused: string;
  settingsTrackingActive: string;
  settingsTrackingSignIn: string;
  settingsExportJson: string;
  settingsExportCsv: string;
  settingsExporting: string;
  settingsDeleteHeading: string;
  settingsDeleteBody: string;
  settingsDeleteButton: string;
  settingsCancelDeleteButton: string;
  settingsDeleteConfirm: string;
  settingsDeleteSuccess: string;
  settingsDeleteCancelled: string;
  settingsDeletionScheduled: (date: string) => string;
  settingsAccountUnavailable: string;
  settingsSending: string;
  settingsSaving: string;
  settingsPauseAll: string;
  settingsResumeAll: string;
  settingsExportComplete: string;
  settingsEmailVerify: string;
  settingsEmailVerified: string;
  settingsEmailUnverified: string;
  settingsSendVerification: string;
  settingsVerificationSent: string;
  settingsMatchReviewHeading: string;
  settingsMatchReviewBody: string;
  settingsMatchReviewEmpty: string;
  settingsMatchReviewSource: string;
  settingsMatchReviewCandidate: string;
  settingsMatchReviewAccept: string;
  settingsMatchReviewDismiss: string;
  settingsMatchReviewSaved: string;
  settingsMatchReviewLoadError: string;
  settingsOpsHeading: string;
  settingsOpsBody: string;
  settingsOpsActiveUsers: string;
  settingsOpsActiveTrackings: string;
  settingsOpsVerifiedOffers: string;
  settingsOpsActiveStores: string;
  settingsOpsPendingOutbox: string;
  settingsOpsScrapeJobs: string;
  settingsOpsStoreHealth: string;
  settingsOpsNoStoreHealth: string;
  previewEyebrow: string;
  previewHeading: string;
  previewListedIn: (currency: string) => string;
  previewPriceLabel: string;
  previewEmailPreferences: string;
  previewEmailTarget: string;
  previewEmailFurther: string;
  previewHelp: string;
  previewCancel: string;
  previewConfirm: string;
  previewSaving: string;
  previewSignInToTrack: string;
  previewTrackingStarted: string;
  previewPriceLabel2: (currency: string) => string;
  previewDecreaseTarget: string;
  previewIncreaseTarget: string;
  previewVerificationTimeout: string;
  previewDetailsFailed: string;
  previewDataUnreadable: string;
  previewStreamInterrupted: string;
  previewInvalidPrice: string;
  previewInvalidCurrency: string;
  previewNoUrl: string;
  previewNoOffer: string;
  authEyebrow: string;
  authLoginTitle: string;
  authRegisterTitle: string;
  authNameLabel: string;
  authEmailLabel: string;
  authPasswordLabel: string;
  authLoginButton: string;
  authRegisterButton: string;
  authWorking: string;
  authGoogleButton: string;
  authForgotPassword: string;
  authNewUser: string;
  authExistingUser: string;
  authCreateAccount: string;
  authSignIn: string;
  authHelp: string;
  authRegisterSuccess: string;
  authGoogleFailure: string;
  authGoogleIncomplete: string;
  authGoogleUnavailable: string;
  authOr: string;
  authSendResetLink: string;
  authVerifyTitle: string;
  authVerifyBody: string;
  authVerifyFailed: string;
  authResetTitle: string;
  authResetSaved: string;
  authResetContinue: string;
  authResetSaving: string;
  authResetNewPassword: string;
  authForgotTitle: string;
  authForgotSent: string;
  authForgotBack: string;
  authContinue: string;
  authPasswordResetConfirm: string;
  authMissingToken: string;
  loading: string;
  loadingProducts: string;
  tryAgain: string;
  close: string;
  priceHistory: string;
  convertedRate: (date: string) => string;
  convertedOriginal: (price: string) => string;
  conversionRates: string;
  mobileNavClose: string;
}

const en: Strings = {
  requestFailed: 'That request failed. Try again.',
  navDashboard: 'Dashboard',
  navDiscover: 'Discover',
  navProducts: 'My products',
  navCompare: 'Compare',
  navNotifications: 'Notifications',
  navSettings: 'Settings',

  sideWorkspace: 'WORKSPACE',
  sideMarket: 'Market',
  sideChooseMarket: 'Choose market',
  sideCollapse: 'Collapse sidebar',
  sideExpand: 'Expand sidebar',

  breadcrumbProductDetails: 'Product details',
  globalSearchPlaceholder: 'Product name or store URL',
  globalSearchLabel: 'Search products',
  searchVerifying: 'Verifying…',
  clearSearch: 'Clear search',
  searchOpenLinkFailed: 'Could not open that product link. Check the URL and try again.',
  searchOpeningLink: 'Opening that product page…',
  searchLinkRequired: 'Enter a complete product link, including https://.',
  navigationLabel: 'Main navigation',
  notificationsLabel: 'Notifications',
  notificationsUnread: (n) => `Notifications, ${n} unread`,
  notificationsDeliveryLabel: 'Email',
  notificationUpdateFailed: 'Could not update this alert. Try again.',
  notificationReadFailed: 'Could not mark the alerts as read. Try again.',
  notificationsDeliveryState: (status) => {
    const states: Record<string, string> = { sent: 'Sent', pending: 'Pending', failed: 'Failed', suppressed: 'Not sent' };
    return states[status.toLowerCase()] || 'Unknown';
  },
  operationsQueueMessages: (count) => `${count.toLocaleString()} messages`,
  operationsQueueConsumers: (count) => `${count.toLocaleString()} consumers`,
  signIn: 'Sign in',
  signOut: 'Sign out',
  signedOut: 'Signed out',
  openNavigation: 'Open navigation',
  closeNavigation: 'Close navigation',

  marketLabel: 'Preferred shopping market',
  marketAll: 'All markets',
  marketChoosePrompt: 'We could not infer your market from your time zone. Choose a market in the header, or keep All markets.',
  marketEG: 'EG · EGP',
  marketSA: 'SA · SAR',
  marketAE: 'UAE · AED',
  chooseMarket: 'Choose market',

  dashboardEyebrow: 'YOUR SHOPPING OVERVIEW',
  dashboardHeading: 'Track prices. Buy at your price.',
  dashboardSubheading: '',
  dashboardExplore: 'Explore products',
  dashboardAddHeading: 'What are you looking for?',
  dashboardAddBody: '',
  dashboardAddPlaceholder: 'Product name or store URL',
  dashboardFindButton: 'Find a product',
  dashboardTrackingHeading: 'Your tracking',
  dashboardTrackingSubheading: '',
  dashboardViewAll: 'View all',
  dashboardTrackedMetric: 'Tracked products',
  dashboardTrackedNote: 'Across your saved offers',
  dashboardAtTargetMetric: 'At target',
  dashboardAtTargetNote: 'Current price reached your target',
  dashboardNearTargetMetric: 'Near target',
  dashboardNearTargetNote: 'Within 10% of target',
  dashboardUnreadMetric: 'Unread updates',
  dashboardUnreadNote: 'In your notification center',
  dashboardRecentHeading: 'Tracked products',
  dashboardRecentSubheading: '',
  dashboardLoadError: 'Could not load your tracked prices.',
  dashboardGuestEyebrow: 'A BETTER WAY TO SHOP',
  dashboardGuestHeading: 'Save products and track their prices.',
  dashboardGuestBody: '',
  dashboardGuestCta: 'Create an account',
  dashboardDataNote: 'Prices shown are the latest observations recorded for tracked offers. Store availability and prices can change.',
  dashboardNothingTracked: 'Nothing tracked yet',
  dashboardNothingTrackedBody: 'Add a product above and set a target price to begin.',
  dashboardTryAgain: 'Try again',
  dashboardTrackingLoadError: 'Could not load tracking.',

  dropsEyebrow: 'OBSERVED PRICE CHANGES',
  dropsHeading: 'Recent price drops',
  dropsSubheading: '',
  dropsDiscoverMore: 'Discover more',
  dropsViewHistory: 'View price history',
  dropsLower: (pct) => `${pct}% LOWER`,

  discoverEyebrow: 'PRODUCT DISCOVERY',
  discoverHeading: 'Find a product and compare store prices.',
  discoverSubheading: '',
  discoverSearchPlaceholder: 'Search by product name, model, or brand',
  discoverSearchLabel: 'Search products',
  discoverSearchButton: 'Search',
  discoverSearching: 'Searching…',
  discoverResultsCount: (n) => `${n} results`,
  discoverResultsLive: (n) => `${n} results so far…`,
  discoverVerifiedPrices: (n) => `${n} verified prices`,
  discoverPriceFilter: 'Price range',
  discoverMinPrice: 'Min',
  discoverMaxPrice: 'Max',
  discoverAnyPrice: 'Any price',
  discoverSortLabel: 'Sort by',
  discoverSortRelevance: 'Relevance',
  discoverSortPrice: 'Price within currency',
  discoverSortPriceAsc: 'Price: Low to High',
  discoverSortPriceDesc: 'Price: High to Low',
  discoverSortVerified: 'Verified offers first',
  discoverSortStore: 'Store: A to Z',
  discoverSortName: 'Product name: A to Z',
  discoverFilterMarket: 'Market',
  discoverFilterAll: 'All markets',
  discoverNote: 'Results arrive as sources finish. Confirm an offer from its store before tracking.',
  discoverVerified: 'PRICE VERIFIED',
  discoverUnverified: 'DISCOVERED · PRICE UNVERIFIED',
  discoverPreviewTrack: 'Preview & track',
  discoverAddCompare: 'Add to compare',
  discoverRemoveCompare: 'Remove from compare',
  discoverInspectStore: 'Inspect store page',
  discoverObservedPrice: 'Observed listed price',
  discoverPriceUnavailable: 'Price unavailable',
  discoverVerifyAtStore: 'Verify at store',
  discoverCurrencyUnavailable: 'Original currency unavailable',
  discoverDiscoverySource: 'Discovery source',
  discoverNoResults: 'Search products across stores',
  discoverNoResultsBody: 'Search for a product to see offers from supported stores.',
  discoverNoResultsMarket: 'No results from this market. Choose another market or show all markets.',
  discoverTryAgain: 'Try search again',
  discoverSomeFailed: 'Some stores did not respond. Showing the results available so far.',
  discoverTemporarilyUnavailable: 'Search is unavailable right now. Try again.',
  discoverTimedOut: 'Search took too long. Try again.',
  discoverListView: 'List view',
  discoverGridView: 'Grid view',
  discoverProvidersProgress: (n) => `Searching providers and stores · ${n} delivered`,
  discoverSearchResultsEyebrow: 'SEARCH RESULTS',
  discoverBestDealsEyebrow: 'BEST DEALS & OFFERS',
  discoverSearchResultsHeading: (query) => `Results for “${query}”`,
  discoverBestDealsHeading: 'Best Deals & Price Drops',
  discoverBack: 'Back to Discover',
  discoverSearchRetryMessage: 'Your search is still here. Retry to request the store results again.',
  discoverTopPriceDrops: 'Today’s Top Price Drops',
  discoverClearPrice: 'Clear',
  discoverPriceRangeError: 'Minimum price must be less than or equal to maximum price.',
  discoverStoreCurrencyLabel: 'store currency',
  discoverSearchingStores: 'Searching stores · Gathering results',
  discoverStillSearching: 'Still scanning stores for more offers…',
  discoverSearchingResults: (found, verified) => `Searching stores · ${found} found · ${verified} verified`,
  discoverInStock: 'Available',
  discoverOutOfStock: 'Out of stock',
  discoverTrackPrice: 'Track price',
  discoverPricePending: 'Price pending',
  discoverPriceUnverified: 'Not verified',
  discoverCompared: 'Compared',
  discoverCompare: 'Compare',
  discoverStore: 'Store',
  discoverSave: 'Save',
  discoverViewDeal: 'View deal',
  discoverNoRecentDrops: 'No recent price drops',
  discoverNoRecentDropsBody: 'New deals will appear here when tracked store prices fall.',
  dealCategoryElectronics: 'Electronics',
  dealCategoryMobiles: 'Mobiles & Tablets',
  dealCategoryComputers: 'Laptops & Computers',
  dealCategoryHome: 'Home & Kitchen',
  dealCategoryGaming: 'Gaming',
  dealCategoryAudio: 'Audio',
  dealCategoryFashion: 'Fashion',
  dealCategoryBeauty: 'Beauty & Health',
  allCategories: 'All categories',
  searchCategoryMobilePhones: 'Mobile phones',
  searchCategoryLaptops: 'Laptops',
  searchCategoryTablets: 'Tablets',
  searchCategoryTvsMonitors: 'TVs & monitors',
  searchCategoryWearables: 'Wearables',
  searchCategoryHomeAppliances: 'Home appliances',
  searchCategoryFurniture: 'Furniture',
  searchCategoryGeneral: 'General',

  overviewTrendingEyebrow: 'RECENT SHOPPER INTEREST',
  overviewTrendingHeading: 'Trending products',
  overviewTrendingBody: () => '',
  overviewCategoriesEyebrow: 'BASED ON SAVED TRACKING',
  overviewCategoriesHeading: 'Popular categories',
  overviewCategoriesBody: () => '',
  overviewProductsEyebrow: 'RECENTLY ADDED PRODUCTS',
  overviewProductsHeading: 'Recently discovered products',
  overviewProductsBody: () => '',
  overviewStoresEyebrow: 'STORES NOW TRACKED',
  overviewStoresHeading: 'Recently added stores',
  overviewStoresBody: () => '',
  overviewNewTracking: (n) => `${n} new tracking${n === 1 ? '' : 's'}`,
  overviewVerifiedOffer: (n) => `${n} verified offer${n === 1 ? '' : 's'}`,
  overviewEmptyHeading: 'No discovery highlights yet.',
  overviewEmptyBody: '',
  overviewHighlightsUnavailable: 'Could not load discovery highlights. Search still works.',

  myProductsEyebrow: 'YOUR COLLECTION',
  myProductsHeading: 'My products',
  myProductsAddProduct: 'Add product',
  myProductsSearchPlaceholder: 'Search products',
  myProductsSearchLabel: 'Search tracked products',
  myProductsStatusFilter: 'Status',
  myProductsAllStatuses: 'All statuses',
  myProductsCategoryFilter: 'Category',
  myProductsAllCategories: 'All categories',
  myProductsMarketFilter: 'Market',
  myProductsAllMarkets: 'All markets',
  myProductsPriceFilter: 'Price',
  myProductsSortBy: 'Sort by',
  myProductsFiltering: 'Filtering your products',
  myProductsSortProductName: 'Product name',
  myProductsSortLowestPrice: 'Lowest price',
  myProductsSortTargetPrice: 'Target price',
  myProductsSortPriceChange: 'Price change',
  myProductsSortBiggestDrop: 'Biggest recent drop',
  myProductsSortTargetProximity: 'Closest to target',
  myProductsSortLastUpdated: 'Last updated',
  myProductsSortTrackingStatus: 'Tracking status',
  myProductsAnyPrice: 'Any price',
  myProductsAtOrBelowTarget: 'At or below target',
  myProductsNearTarget: 'Within 10% above target',
  myProductsAboveTarget: 'More than 10% above target',
  myProductsPriceUnavailable: 'Price unavailable',
  myProductsShowPaused: 'Show paused',
  myProductsTable: 'Table',
  myProductsCards: 'Cards',
  myProductsViewLabel: 'Product display',
  myProductsSelected: (n) => `${n} selected`,
  myProductsResume: 'Resume',
  myProductsPause: 'Pause',
  myProductsRemoveSelected: 'Remove selected',
  myProductsNoMatch: 'No products match',
  myProductsNoMatchBody: 'Change your search or add another product.',
  myProductsEmpty: 'No tracked products yet',
  myProductsEmptyBody: 'Add a product and target price to start tracking.',
  myProductsLoadError: 'Could not load your products.',
  myProductsDataNote: 'Pausing keeps your tracking. Removing it stops price checks; price history stays saved.',
  myProductsViewProduct: 'View product',
  myProductsSelectLabel: (name) => `Select ${name}`,
  myProductsSelectAll: 'Select all visible products',
  myProductsBulkFailed: (n) => `Could not complete ${n} selected action${n === 1 ? '' : 's'}.`,
  myProductsBulkRemovedMsg: 'Selected tracking removed.',
  myProductsBulkUpdatedMsg: 'Selected tracking updated.',

  colProduct: 'Product',
  colStore: 'Best store',
  colCurrentPrice: 'Current price',
  colTargetPrice: 'Target price',
  colDifference: 'Price difference',
  colStatus: 'Status',
  colActions: 'Actions',
  sortBy: (label) => `Sort by ${label}`,
  atTarget: 'At target',
  statusTracking: 'Tracking',
  statusPaused: 'Paused',
  pauseTracking: 'Pause tracking',
  resumeTracking: 'Resume tracking',
  removeTracking: 'Remove tracking',
  removeTrackingConfirm: 'Remove this tracked product and its target?',
  trackingRemoved: 'Tracking removed.',
  trackingResumed: 'Tracking resumed.',
  trackingPaused: 'Tracking paused.',
  trackingPreferencesHeading: 'Target and email preferences',
  trackingSavePreferences: 'Save preferences',
  trackingEmailTarget: 'Email me when the target is reached',
  trackingEmailFurther: 'Email me about later price decreases',
  trackingPreferencesSaved: 'Tracking preferences saved.',
  actionFailed: 'Could not complete that action. Try again.',

  compareEyebrow: 'PRODUCT COMPARISON',
  compareHeading: 'Compare products',
  compareLoading: 'Loading comparison…',
  compareSubheading: '',
  compareEmpty: 'No products to compare',
  compareEmptyBody: 'Use "Add to compare" on any product to add it here.',
  compareRemove: 'Remove from comparison',
  compareViewProduct: 'View product',
  compareLatestPrice: 'Latest price',
  compareTargetPrice: 'Your target',
  compareOfferCount: (n) => `${n} verified offer${n === 1 ? '' : 's'}`,
  compareOffers: 'Verified offers',
  compareBestStore: 'Lowest verified offer',
  compareMarket: 'Market',
  compareNotTracked: 'Not tracked in this account',
  compareHistoryHeading: 'Price history',
  compareHistoryIntro: 'Compare recorded prices for each product in its listed currency.',
  compareHistoryPeriod: 'Period',
  compareHistoryPeriod30: '30 days',
  compareHistoryPeriod90: '90 days',
  compareHistoryPeriod365: '1 year',
  compareHistoryPeriodAll: 'All time',
  compareHistoryStores: 'Show compared store histories',
  compareHistorySampled: 'Showing representative price history from the beginning, middle, and latest part of this period.',
  compareHistoryRange: (from, to, currency) => `Price history from ${from} to ${to} in ${currency}`,
  compareHistoryUnavailable: (currency) => `No price history is saved in ${currency} for this period.`,
  compareNoHistoryForCurrency: (currency) => `No comparable store history is available in ${currency}.`,
  compareProduct: 'Product',
  compareBrand: 'Brand',
  compareCategory: 'Category',
  compareUnavailable: 'Unavailable',
  compareUnavailableNote: 'Unavailable means the product record does not include that value. Prices retain their original currencies.',
  compareBackToProducts: 'Return to products',
  compareError: 'Could not load this product. Try again.',
  storeOffersHeading: 'Store offers',
  dealQualityHeading: 'Deal score',
  dealQualityInsufficient: 'Insufficient history',
  dealQualityScoreLabel: (score: number) => `${score}/100`,
  dealQualityTooltip: 'Based on historical price, proximity to the observed low, price stability, history coverage, and check recency. It does not verify store authenticity or guarantee a deal.',
  productDetailsUnavailable: 'Product details unavailable',
  productDetailsLoading: 'Loading product and store offers…',
  productOffersCount: (n) => `${n} offer${n === 1 ? '' : 's'}`,
  productMarketCurrency: 'Market currency',
  productLastChecked: 'Last checked',
  productMonitoring: 'Monitoring',
  productActiveListing: 'Active listing',
  productInactiveListing: 'Inactive listing',
  productIncludeOffer: 'Include offer',
  productExcludeOffer: 'Exclude offer',
  productOfferIncluded: (store) => `${store} offer included in monitoring.`,
  productOfferExcluded: (store) => `${store} offer excluded from monitoring.`,
  productExcluded: 'Excluded',
  productIncluded: 'Included',
  productNoStoredOffers: 'No stored offers',
  productNoStoredOffersBody: 'No store listings are currently linked to this product.',
  productHistoryPeriod7: '7 days',
  productHistoryByStore: 'Show price history by store',
  productHistoryPartial: 'Some offer histories could not be loaded; the observations shown are partial.',
  productHistorySampled: 'Showing representative observations from the beginning, midpoint, and latest part of this range.',
  productHistoryChartLabel: (from, to, min, max) => `Price observations from ${from} to ${to}. Prices range from ${min} to ${max}.`,
  productTrackingActivity: 'Tracking activity',
  productTrackingStarted: 'Tracking started',
  productNavOffers: 'Offers',
  productNavHistory: 'Price history',
  productNavActivity: 'Tracking activity',
  productActivityEmpty: 'No tracking activity yet.',
  productLowestVerified: 'Lowest verified offer by day',
  productHistoryEmpty: 'No price history is available for these offers.',
  productHistoryEmptyBody: 'Price observations will appear after these stores are checked.',
  productHistoryLoadFailed: 'Could not load price history.',
  dashFeedEyebrow: 'Live from the catalog',
  dashFeedHeading: 'Fresh finds',
  dashFeedWas: 'was',
  dashFeedMovementDown: (percent, date) => `down ${percent}% since ${date}`,
  dashFeedMovementUp: (percent, date) => `up ${percent}% since ${date}`,
  dashFeedStores: (n) => `${n} ${n === 1 ? 'store' : 'stores'}`,
  dashFeedLoading: 'Pulling more finds…',
  dashFeedMore: 'More finds',
  dashFeedEnd: 'You have seen every product in this session.',
  dashFeedReshuffle: 'Shuffle again',
  dashFeedError: 'The feed hit a snag on the way back.',
  dashFeedEmpty: 'No finds yet',
  dashFeedEmptyBody: 'Nothing is in the catalog right now. Check back once the first scrape lands.',
  dashFeedOpen: (name) => `Open ${name}`,
  productMovementDown: (percent, date) => `down ${percent}% since ${date}`,
  productMovementUp: (percent, date) => `up ${percent}% since ${date}`,
  productOfferBest: 'Best verified price',
  productOfferVerified: 'Verified',
  productOfferStale: 'Stale',
  productOfferUnverified: 'Unverified',
  productOfferNoPrice: 'No price',
  productOfferUnverifiedHint: 'This offer has not returned a confirmed price.',
  productOfferVerifiedHint: 'A successful scrape returned this current price.',
  productOfferStaleHint: 'This price has not been re-checked recently.',
  productOfferNoPriceHint: 'No successful price observation is stored for this offer.',
  productPriceNotConfirmed: 'No confirmed current price',
  productPriceSource: (store) => `Verified at ${store}`,
  productImageMissing: 'No product photo available.',
  notFoundAriaLabel: 'Page not found',
  notFoundStamp: 'EXPIRED DEAL',
  notFoundEyebrow: 'OFFER NOT FOUND',
  notFoundHeading: 'Product or offer not found',
  notFoundDescription: 'The page may have moved or the offer may have expired.',
  notFoundObservation: 'Observation',
  notFoundStatus: 'Status',
  notFoundStatusValue: 'Out of stock',
  notFoundRecommendation: 'Recommendation',
  notFoundExploreDeals: 'Explore best deals',
  notFoundDashboard: 'Back to Dashboard',
  skipToContent: 'Skip to content',

  notificationsEyebrow: 'YOUR ALERTS',
  notificationsHeading: 'Notifications',
  notificationsEmpty: 'No notifications yet',
  notificationsEmptyBody: 'Track a product and set a target price to receive alerts.',
  notificationsMarkRead: 'Mark as read',
  notificationsMarkAllRead: 'Mark all read',
  notificationsFilterAll: 'All',
  notificationsFilterUnread: 'Unread',
  notificationsFilterRead: 'Read',
  notificationsShowFilter: 'Show',
  notificationsFilterType: 'Type',
  notificationsFilterTarget: 'Target reached',
  notificationsFilterFurther: 'Further price decrease',
  notificationsFilterStopped: 'Tracking stopped',
  notificationsTargetReached: 'Target reached',
  notificationsFurtherDrop: 'Further decrease',
  notificationsTrackingStopped: 'We could not confirm this offer’s price, so tracking has stopped.',
  notificationsAt: (price, target) => `${price} · target ${target}`,
  notificationsFrom: (store) => `from ${store}`,
  notificationsEquivalent: 'converted equivalent',
  notificationsLoadError: 'Could not load notifications.',
  notificationsLoadErrorBody: 'Your notifications are unchanged. Try again.',

  settingsEyebrow: 'PREFERENCES',
  settingsHeading: 'Settings',
  settingsSubheading: '',
  settingsAccountHeading: 'Account',
  settingsAccountGuest: 'Sign in to view account settings.',
  guestPageAccessBody: 'Sign in to see your saved products and notifications.',
  guestPageAccessHeading: 'Sign in to continue',
  settingsAccountAccess: 'Account access',
  settingsAppearanceHeading: 'Appearance',
  settingsAppearanceSubheading: '',
  settingsTheme: 'Theme',
  settingsThemeLight: 'Light',
  settingsThemeDark: 'Dark',
  settingsThemeSystem: 'System',
  settingsLanguage: 'Language',
  settingsLanguageEn: 'English',
  settingsLanguageAr: 'العربية',
  settingsMarketsHeading: 'Markets and currency',
  settingsMarketsSubheading: '',
  settingsPreferredMarket: 'Preferred shopping market',
  settingsDisplayCurrency: 'Display currency',
  settingsCurrencyMarket: 'Use the selected market currency',
  settingsChooseMarket: 'Choose a market',
  settingsMarketEgypt: 'Egypt · EGP',
  settingsMarketSaudi: 'Saudi Arabia · SAR',
  settingsMarketUAE: 'United Arab Emirates · AED',
  settingsNotificationsHeading: 'Notifications',
  settingsNotificationsSubheading: '',
  settingsNotificationsPerProduct: 'Per-product preferences',
  settingsTrackingHeading: 'Search and tracking',
  settingsTrackingSubheading: '',
  settingsTrackingPaused: 'All price monitoring is currently paused. Resume to receive new alerts.',
  settingsTrackingActive: 'Tracking is active',
  settingsTrackingSignIn: 'Sign in to manage',
  settingsExportJson: 'Export JSON',
  settingsExportCsv: 'Export CSV',
  settingsExporting: 'Exporting…',
  settingsDeleteHeading: 'Delete account',
  settingsDeleteBody: '',
  settingsDeleteButton: 'Request account deletion',
  settingsCancelDeleteButton: 'Cancel deletion request',
  settingsDeleteConfirm: 'Request account deletion? Your account will be deactivated now and permanently deleted after the grace period.',
  settingsDeleteSuccess: 'Deletion requested. Tracking is paused during the grace period.',
  settingsDeleteCancelled: 'Account deletion request cancelled.',
  settingsDeletionScheduled: (date) => `Deletion is scheduled for ${date}. Sign in during the grace period to cancel.`,
  settingsAccountUnavailable: 'Account details are unavailable.',
  settingsSending: 'Sending…',
  settingsSaving: 'Saving…',
  settingsPauseAll: 'Pause all tracking',
  settingsResumeAll: 'Resume all tracking',
  settingsExportComplete: 'Account export downloaded.',
  settingsEmailVerify: 'Email address',
  settingsEmailVerified: 'Verified',
  settingsEmailUnverified: 'Not verified — email alerts are disabled',
  settingsSendVerification: 'Send verification email',
  settingsVerificationSent: 'Verification email sent.',
  settingsMatchReviewHeading: 'Product match review',
  settingsMatchReviewBody: '',
  settingsMatchReviewEmpty: 'No product matches need review.',
  settingsMatchReviewSource: 'Tracked product',
  settingsMatchReviewCandidate: 'Verified candidate',
  settingsMatchReviewAccept: 'Accept match',
  settingsMatchReviewDismiss: 'Dismiss',
  settingsMatchReviewSaved: 'Match review resolved.',
  settingsMatchReviewLoadError: 'Could not load product match reviews. Try again.',
  settingsOpsHeading: 'Operations overview',
  settingsOpsBody: '',
  settingsOpsActiveUsers: 'Active accounts',
  settingsOpsActiveTrackings: 'Active tracking rules',
  settingsOpsVerifiedOffers: 'Active offers',
  settingsOpsActiveStores: 'Active stores',
  settingsOpsPendingOutbox: 'Unpublished messages',
  settingsOpsScrapeJobs: 'Scrapes in the last 24 hours',
  settingsOpsStoreHealth: 'Store scrape health',
  settingsOpsNoStoreHealth: 'No stores are configured.',

  previewEyebrow: 'PRODUCT PREVIEW',
  previewHeading: 'Review product before tracking',
  previewListedIn: (currency) => `listed in ${currency}`,
  previewPriceLabel: 'Your target price',
  previewPriceLabel2: (currency) => `Your target price (${currency})`,
  previewEmailPreferences: 'Email preferences',
  previewEmailTarget: 'Email me when the target is reached',
  previewEmailFurther: 'Email me on later price decreases',
  previewHelp: 'The product will be saved after confirmation. The listed amount is not treated as a verified price observation.',
  previewCancel: 'Cancel',
  previewConfirm: 'Confirm tracking',
  previewSaving: 'Saving…',
  previewSignInToTrack: 'Sign in to track',
  previewTrackingStarted: 'Tracking started. Your target is saved.',
  previewDecreaseTarget: 'Decrease target price by one',
  previewIncreaseTarget: 'Increase target price by one',
  previewVerificationTimeout: 'Product details are taking longer than expected. Try the link again.',
  previewDetailsFailed: 'Could not read this product page.',
  previewDataUnreadable: 'The product details could not be read.',
  previewStreamInterrupted: 'Product details stopped loading before finishing. Try again.',
  previewInvalidPrice: 'Enter a target price greater than zero.',
  previewInvalidCurrency: 'This store’s currency could not be identified. This offer cannot be tracked yet.',
  previewNoUrl: 'This result has no supported product URL to track.',
  previewNoOffer: 'No active offer with a matching source currency is available to track.',

  authEyebrow: 'DEALIO',
  authLoginTitle: 'Welcome back',
  authRegisterTitle: 'Create your account',
  authNameLabel: 'Name',
  authEmailLabel: 'Email',
  authPasswordLabel: 'Password',
  authLoginButton: 'Sign in',
  authRegisterButton: 'Create account',
  authWorking: 'Please wait…',
  authGoogleButton: 'Continue with Google',
  authForgotPassword: 'Forgot password?',
  authNewUser: 'New to Dealio?',
  authExistingUser: 'Already have an account?',
  authCreateAccount: 'Create account',
  authSignIn: 'Sign in',
  authHelp: 'You can start tracking after registration. Email verification is needed for email notifications.',
  authRegisterSuccess: 'Account created. You can track now; verify your email to receive email alerts.',
  authGoogleFailure: 'Could not sign in with Google. Try again.',
  authGoogleIncomplete: 'Google sign-in did not finish. Try again.',
  authGoogleUnavailable: 'Google sign-in is not configured for this app yet.',
  authOr: 'or',
  authSendResetLink: 'Send reset link',

  authVerifyTitle: 'Verify your email',
  authVerifyBody: 'Your email is verified. Email alerts can now be delivered.',
  authVerifyFailed: 'This verification link is no longer valid.',
  authResetTitle: 'Choose a new password',
  authResetSaved: 'Your password has been changed.',
  authResetContinue: 'Continue to sign in',
  authResetSaving: 'Saving…',
  authResetNewPassword: 'New password',
  authForgotTitle: 'Reset your password',
  authForgotSent: 'If that address belongs to an account, a reset link has been sent.',
  authForgotBack: 'Back to Dealio',
  authContinue: 'Continue to Dealio',
  authPasswordResetConfirm: 'This password reset link is missing its token.',
  authMissingToken: 'This verification link is missing its token.',

  loading: 'Loading…',
  loadingProducts: 'Loading tracked products',
  tryAgain: 'Try again',
  close: 'Close',
  priceHistory: 'Price history',
  convertedRate: (date) => `rate as of ${date}`,
  convertedOriginal: (price) => `Original ${price}`,
  conversionRates: 'rates',
  mobileNavClose: 'Close navigation',
};

const ar: Strings = {
  requestFailed: 'تعذّر تنفيذ الطلب. حاول مرة أخرى.',
  navDashboard: 'لوحة التحكم',
  navDiscover: 'اكتشف',
  navProducts: 'منتجاتي',
  navCompare: 'مقارنة',
  navNotifications: 'الإشعارات',
  navSettings: 'الإعدادات',

  sideWorkspace: 'مساحة العمل',
  sideMarket: 'السوق',
  sideChooseMarket: 'اختر السوق',
  sideCollapse: 'طي الشريط الجانبي',
  sideExpand: 'توسيع الشريط الجانبي',

  breadcrumbProductDetails: 'تفاصيل المنتج',
  globalSearchPlaceholder: 'اسم المنتج أو رابط المتجر',
  globalSearchLabel: 'ابحث عن المنتجات',
  searchVerifying: 'جارٍ التحقق…',
  clearSearch: 'مسح البحث',
  searchOpenLinkFailed: 'تعذّر فتح رابط المنتج. تحقّق من الرابط وحاول مرة أخرى.',
  searchOpeningLink: 'جارٍ فتح صفحة المنتج…',
  searchLinkRequired: 'أدخل رابط منتج كاملاً يبدأ بـ https://.',
  navigationLabel: 'القائمة الرئيسية',
  notificationsLabel: 'الإشعارات',
  notificationsUnread: (n) => `الإشعارات، ${n} غير مقروء`,
  notificationsDeliveryLabel: 'البريد الإلكتروني',
  notificationUpdateFailed: 'تعذّر تحديث هذا التنبيه. حاول مرة أخرى.',
  notificationReadFailed: 'تعذّر تحديد التنبيهات كمقروءة. حاول مرة أخرى.',
  notificationsDeliveryState: (status) => {
    const states: Record<string, string> = { sent: 'أُرسل', pending: 'قيد الإرسال', failed: 'فشل الإرسال', suppressed: 'لم يُرسل' };
    return states[status.toLowerCase()] || 'غير معروف';
  },
  operationsQueueMessages: (count) => `${count.toLocaleString('ar-EG')} رسالة`,
  operationsQueueConsumers: (count) => `${count.toLocaleString('ar-EG')} مستهلك`,
  signIn: 'تسجيل الدخول',
  signOut: 'تسجيل الخروج',
  signedOut: 'تم تسجيل الخروج',
  openNavigation: 'فتح التنقل',
  closeNavigation: 'إغلاق التنقل',

  marketLabel: 'سوق التسوق المفضل',
  marketAll: 'كل الأسواق',
  marketChoosePrompt: 'تعذر تحديد سوقك من المنطقة الزمنية. اختر سوقاً من الشريط العلوي أو تابع بعرض كل الأسواق.',
  marketEG: 'مصر · ج.م.',
  marketSA: 'السعودية · ر.س.',
  marketAE: 'الإمارات · د.إ.',
  chooseMarket: 'اختر السوق',

  dashboardEyebrow: 'نظرة عامة على تسوقك',
  dashboardHeading: 'تابع الأسعار واشترِ بالسعر المناسب لك.',
  dashboardSubheading: '',
  dashboardExplore: 'استكشف المنتجات',
  dashboardAddHeading: 'ماذا تبحث عن؟',
  dashboardAddBody: '',
  dashboardAddPlaceholder: 'اسم المنتج أو رابط المتجر',
  dashboardFindButton: 'ابحث عن منتج',
  dashboardTrackingHeading: 'متابعتك',
  dashboardTrackingSubheading: '',
  dashboardViewAll: 'عرض الكل',
  dashboardTrackedMetric: 'منتجات متابَعة',
  dashboardTrackedNote: 'عبر عروضك المحفوظة',
  dashboardAtTargetMetric: 'وصل للهدف',
  dashboardAtTargetNote: 'السعر الحالي وصل لهدفك',
  dashboardNearTargetMetric: 'قريب من الهدف',
  dashboardNearTargetNote: 'في حدود 10% من الهدف',
  dashboardUnreadMetric: 'تحديثات غير مقروءة',
  dashboardUnreadNote: 'في مركز الإشعارات',
  dashboardRecentHeading: 'المنتجات المتابَعة',
  dashboardRecentSubheading: '',
  dashboardLoadError: 'تعذّر تحميل الأسعار التي تتابعها.',
  dashboardGuestEyebrow: 'طريقة تسوق أفضل',
  dashboardGuestHeading: 'احفظ المنتجات وتابع أسعارها.',
  dashboardGuestBody: '',
  dashboardGuestCta: 'إنشاء حساب',
  dashboardDataNote: 'الأسعار المعروضة هي أحدث قراءات مسجّلة للعروض المتابَعة. قد تتغير التوافرية والأسعار.',
  dashboardNothingTracked: 'لا يوجد شيء متابَع بعد',
  dashboardNothingTrackedBody: 'أضف منتجاً بالأعلى وحدد سعراً مستهدفاً للبدء.',
  dashboardTryAgain: 'حاول مجدداً',
  dashboardTrackingLoadError: 'تعذّر تحميل بيانات المتابعة.',

  dropsEyebrow: 'تغيرات الأسعار المرصودة',
  dropsHeading: 'آخر تخفيضات الأسعار',
  dropsSubheading: '',
  dropsDiscoverMore: 'اكتشف المزيد',
  dropsViewHistory: 'عرض تاريخ السعر',
  dropsLower: (pct) => `أقل بـ ${pct}%`,

  discoverEyebrow: 'اكتشاف المنتجات',
  discoverHeading: 'ابحث عن المنتجات وقارن عروض المتاجر.',
  discoverSubheading: '',
  discoverSearchPlaceholder: 'ابحث باسم المنتج أو الطراز أو العلامة التجارية',
  discoverSearchLabel: 'البحث عن المنتجات',
  discoverSearchButton: 'بحث',
  discoverSearching: 'جارٍ البحث…',
  discoverResultsCount: (n) => `${n} نتيجة`,
  discoverResultsLive: (n) => `${n} نتيجة حتى الآن…`,
  discoverVerifiedPrices: (n) => `${n} سعر موثّق`,
  discoverPriceFilter: 'نطاق السعر',
  discoverMinPrice: 'الأدنى',
  discoverMaxPrice: 'الأقصى',
  discoverAnyPrice: 'أي سعر',
  discoverSortLabel: 'ترتيب حسب',
  discoverSortRelevance: 'الصلة',
  discoverSortPrice: 'السعر ضمن العملة',
  discoverSortPriceAsc: 'السعر: من الأقل للأعلى',
  discoverSortPriceDesc: 'السعر: من الأعلى للأقل',
  discoverSortVerified: 'العروض الموثقة أولاً',
  discoverSortStore: 'المتجر: أبجدياً',
  discoverSortName: 'اسم المنتج: أبجدياً',
  discoverFilterMarket: 'السوق',
  discoverFilterAll: 'كل الأسواق',
  discoverNote: 'تظهر النتائج فور وصولها. تحقق من العرض في المتجر قبل المتابعة.',
  discoverVerified: 'السعر موثّق',
  discoverUnverified: 'مكتشف · السعر غير موثّق',
  discoverPreviewTrack: 'معاينة ومتابعة',
  discoverAddCompare: 'إضافة للمقارنة',
  discoverRemoveCompare: 'حذف من المقارنة',
  discoverInspectStore: 'فتح صفحة المتجر',
  discoverObservedPrice: 'السعر المعروض المُسجَّل',
  discoverPriceUnavailable: 'السعر غير متاح',
  discoverVerifyAtStore: 'تحقق في المتجر',
  discoverCurrencyUnavailable: 'العملة الأصلية غير متاحة',
  discoverDiscoverySource: 'مصدر الاكتشاف',
  discoverNoResults: 'ابحث في المنتجات عبر المتاجر',
  discoverNoResultsBody: 'ابحث عن منتج لعرض العروض المتاحة في المتاجر المدعومة.',
  discoverNoResultsMarket: 'لا نتائج من هذا السوق. اختر سوقاً آخر أو اعرض كل الأسواق.',
  discoverTryAgain: 'أعد البحث',
  discoverSomeFailed: 'لم تستجب بعض المتاجر. هذه النتائج المتاحة حتى الآن.',
  discoverTemporarilyUnavailable: 'البحث غير متاح الآن. حاول مرة أخرى.',
  discoverTimedOut: 'استغرق البحث وقتاً طويلاً. حاول مرة أخرى.',
  discoverListView: 'عرض قائمة',
  discoverGridView: 'عرض شبكي',
  discoverProvidersProgress: (n) => `جارٍ البحث في المزودين والمتاجر · ${n} واردة`,
  discoverSearchResultsEyebrow: 'نتائج البحث',
  discoverBestDealsEyebrow: 'أفضل العروض والأسعار',
  discoverSearchResultsHeading: (query) => `نتائج البحث عن «${query}»`,
  discoverBestDealsHeading: 'أفضل العروض وتخفيضات الأسعار',
  discoverBack: 'العودة إلى صفحة الاكتشاف',
  discoverSearchRetryMessage: 'بحثك محفوظ. أعد المحاولة لطلب نتائج المتاجر من جديد.',
  discoverTopPriceDrops: 'أبرز تخفيضات اليوم',
  discoverClearPrice: 'مسح',
  discoverPriceRangeError: 'يجب أن يكون الحد الأدنى مساوياً للحد الأقصى أو أقل منه.',
  discoverStoreCurrencyLabel: 'عملة المتجر',
  discoverSearchingStores: 'جارٍ البحث في المتاجر · جمع النتائج',
  discoverStillSearching: 'لا يزال البحث جارياً — يتم تحميل المزيد من العروض…',
  discoverSearchingResults: (found, verified) => `جارٍ البحث في المتاجر · ${found} نتيجة · ${verified} موثّقة`,
  discoverInStock: 'متوفر',
  discoverOutOfStock: 'غير متوفر',
  discoverTrackPrice: 'متابعة السعر',
  discoverPricePending: 'السعر قيد التحقق',
  discoverPriceUnverified: 'غير موثّق',
  discoverCompared: 'تمت المقارنة',
  discoverCompare: 'مقارنة',
  discoverStore: 'المتجر',
  discoverSave: 'وفّر',
  discoverViewDeal: 'عرض الصفقة',
  discoverNoRecentDrops: 'لا توجد تخفيضات حديثة',
  discoverNoRecentDropsBody: 'ستظهر العروض الجديدة هنا عند انخفاض أسعار المتاجر التي تتابعها.',
  dealCategoryElectronics: 'إلكترونيات',
  dealCategoryMobiles: 'هواتف وأجهزة لوحية',
  dealCategoryComputers: 'أجهزة كمبيوتر ومحمولة',
  dealCategoryHome: 'المنزل والمطبخ',
  dealCategoryGaming: 'ألعاب',
  dealCategoryAudio: 'صوتيات',
  dealCategoryFashion: 'أزياء',
  dealCategoryBeauty: 'الجمال والصحة',
  allCategories: 'كل الفئات',
  searchCategoryMobilePhones: 'هواتف محمولة',
  searchCategoryLaptops: 'أجهزة محمولة',
  searchCategoryTablets: 'أجهزة لوحية',
  searchCategoryTvsMonitors: 'تلفزيونات وشاشات',
  searchCategoryWearables: 'أجهزة قابلة للارتداء',
  searchCategoryHomeAppliances: 'أجهزة منزلية',
  searchCategoryFurniture: 'أثاث',
  searchCategoryGeneral: 'عام',

  overviewTrendingEyebrow: 'اهتمام المتسوقين الأخير',
  overviewTrendingHeading: 'المنتجات الرائجة',
  overviewTrendingBody: () => '',
  overviewCategoriesEyebrow: 'بناءً على المتابعات المحفوظة',
  overviewCategoriesHeading: 'الفئات الشائعة',
  overviewCategoriesBody: () => '',
  overviewProductsEyebrow: 'منتجات مضافة حديثًا',
  overviewProductsHeading: 'منتجات مكتشفة حديثاً',
  overviewProductsBody: () => '',
  overviewStoresEyebrow: 'متاجر مضافة حديثًا',
  overviewStoresHeading: 'متاجر أُضيفت حديثاً',
  overviewStoresBody: () => '',
  overviewNewTracking: (n) => `${n} متابعة جديدة`,
  overviewVerifiedOffer: (n) => `${n} عرض موثّق`,
  overviewEmptyHeading: 'لا توجد أبرز منتجات بعد.',
  overviewEmptyBody: '',
  overviewHighlightsUnavailable: 'تعذّر تحميل أبرز المنتجات. ما زال بإمكانك البحث.',

  myProductsEyebrow: 'مجموعتك',
  myProductsHeading: 'منتجاتي',
  myProductsAddProduct: 'إضافة منتج',
  myProductsSearchPlaceholder: 'البحث في المنتجات',
  myProductsSearchLabel: 'البحث في المنتجات المتابَعة',
  myProductsStatusFilter: 'الحالة',
  myProductsAllStatuses: 'كل الحالات',
  myProductsCategoryFilter: 'الفئة',
  myProductsAllCategories: 'كل الفئات',
  myProductsMarketFilter: 'السوق',
  myProductsAllMarkets: 'كل الأسواق',
  myProductsPriceFilter: 'السعر',
  myProductsSortBy: 'ترتيب حسب',
  myProductsFiltering: 'جارٍ تصفية منتجاتك',
  myProductsSortProductName: 'اسم المنتج',
  myProductsSortLowestPrice: 'أقل سعر',
  myProductsSortTargetPrice: 'السعر المستهدف',
  myProductsSortPriceChange: 'تغيّر السعر',
  myProductsSortBiggestDrop: 'أكبر انخفاض حديث',
  myProductsSortTargetProximity: 'الأقرب إلى السعر المستهدف',
  myProductsSortLastUpdated: 'آخر تحديث',
  myProductsSortTrackingStatus: 'حالة التتبع',
  myProductsAnyPrice: 'كل الأسعار',
  myProductsAtOrBelowTarget: 'عند الهدف أو أقل',
  myProductsNearTarget: 'أعلى من الهدف حتى 10٪',
  myProductsAboveTarget: 'أعلى من الهدف بأكثر من 10٪',
  myProductsPriceUnavailable: 'السعر غير متاح',
  myProductsShowPaused: 'إظهار المتوقفة',
  myProductsTable: 'جدول',
  myProductsCards: 'بطاقات',
  myProductsViewLabel: 'طريقة عرض المنتجات',
  myProductsSelected: (n) => `${n} محدد`,
  myProductsResume: 'استئناف',
  myProductsPause: 'إيقاف',
  myProductsRemoveSelected: 'حذف المحدد',
  myProductsNoMatch: 'لا توجد منتجات مطابقة',
  myProductsNoMatchBody: 'غيّر بحثك أو أضف منتجاً آخر.',
  myProductsEmpty: 'لا منتجات متابَعة بعد',
  myProductsEmptyBody: 'أضف منتجاً وسعراً مستهدفاً لبدء المتابعة.',
  myProductsLoadError: 'تعذّر تحميل منتجاتك.',
  myProductsDataNote: 'إيقاف المتابعة يحفظ بياناتها. حذفها يوقف فحص الأسعار، مع الاحتفاظ بسجل الأسعار.',
  myProductsViewProduct: 'عرض المنتج',
  myProductsSelectLabel: (name) => `تحديد ${name}`,
  myProductsSelectAll: 'تحديد كل المنتجات المرئية',
  myProductsBulkFailed: (n) => `تعذّر تنفيذ ${n} من الإجراءات المحددة.`,
  myProductsBulkRemovedMsg: 'تمت إزالة المتابعة المحددة.',
  myProductsBulkUpdatedMsg: 'تم تحديث المتابعة المحددة.',

  colProduct: 'المنتج',
  colStore: 'أفضل متجر',
  colCurrentPrice: 'السعر الحالي',
  colTargetPrice: 'السعر المستهدف',
  colDifference: 'فرق السعر',
  colStatus: 'الحالة',
  colActions: 'إجراءات',
  sortBy: (label) => `ترتيب حسب ${label}`,
  atTarget: 'وصل للهدف',
  statusTracking: 'متابَع',
  statusPaused: 'متوقف',
  pauseTracking: 'إيقاف المتابعة',
  resumeTracking: 'استئناف المتابعة',
  removeTracking: 'حذف المتابعة',
  removeTrackingConfirm: 'حذف هذا المنتج المتابَع وهدفه؟',
  trackingRemoved: 'تمت إزالة المتابعة.',
  trackingResumed: 'تم استئناف المتابعة.',
  trackingPaused: 'تم إيقاف المتابعة.',
  trackingPreferencesHeading: 'السعر المستهدف وتفضيلات البريد',
  trackingSavePreferences: 'حفظ التفضيلات',
  trackingEmailTarget: 'أرسل بريداً عند بلوغ السعر المستهدف',
  trackingEmailFurther: 'أرسل بريداً عند الانخفاضات اللاحقة',
  trackingPreferencesSaved: 'تم حفظ تفضيلات المتابعة.',
  actionFailed: 'تعذّر تنفيذ الإجراء. حاول مرة أخرى.',

  compareEyebrow: 'مقارنة المنتجات',
  compareHeading: 'قارن المنتجات',
  compareLoading: 'جارٍ تحميل المقارنة…',
  compareSubheading: '',
  compareEmpty: 'لا توجد منتجات للمقارنة',
  compareEmptyBody: 'استخدم "إضافة للمقارنة" في أي منتج لإضافته هنا.',
  compareRemove: 'حذف من المقارنة',
  compareViewProduct: 'عرض المنتج',
  compareLatestPrice: 'آخر سعر',
  compareTargetPrice: 'سعرك المستهدف',
  compareOfferCount: (n) => `${n} عرض موثّق`,
  compareOffers: 'العروض الموثّقة',
  compareBestStore: 'أقل عرض موثّق',
  compareMarket: 'السوق',
  compareNotTracked: 'غير متابَع في هذا الحساب',
  compareHistoryHeading: 'تاريخ السعر',
  compareHistoryIntro: 'قارن الأسعار المسجلة لكل منتج بعملته الأصلية.',
  compareHistoryPeriod: 'الفترة',
  compareHistoryPeriod30: '٣٠ يوماً',
  compareHistoryPeriod90: '٩٠ يوماً',
  compareHistoryPeriod365: 'سنة واحدة',
  compareHistoryPeriodAll: 'كل الفترات',
  compareHistoryStores: 'عرض سجلات أسعار المتاجر المقارَنة',
  compareHistorySampled: 'نعرض سجلاً تمثيلياً للأسعار من بداية الفترة ووسطها وأحدثها.',
  compareHistoryRange: (from, to, currency) => `تاريخ السعر من ${from} إلى ${to} بـ ${currency}`,
  compareHistoryUnavailable: (currency) => `لا يوجد سجل أسعار محفوظ بعملة ${currency} لهذه الفترة.`,
  compareNoHistoryForCurrency: (currency) => `لا يتوفر سجل أسعار لمتجر قابل للمقارنة بـ ${currency}.`,
  compareProduct: 'المنتج',
  compareBrand: 'العلامة التجارية',
  compareCategory: 'الفئة',
  compareUnavailable: 'غير متاح',
  compareUnavailableNote: 'تعني «غير متاح» أن سجل المنتج لا يتضمن هذه القيمة. تظل الأسعار بعملاتها الأصلية.',
  compareBackToProducts: 'العودة إلى المنتجات',
  compareError: 'تعذّر تحميل هذا المنتج. حاول مرة أخرى.',
  storeOffersHeading: 'عروض المتاجر',
  dealQualityHeading: 'تقييم الصفقة',
  dealQualityInsufficient: 'سجل الأسعار غير كافٍ',
  dealQualityScoreLabel: (score: number) => `${score}/١٠٠`,
  dealQualityTooltip: 'يعتمد على الأسعار التاريخية والقرب من أقل سعر مرصود واستقرار السعر وتغطية السجل وحداثة التحقق. لا يؤكد موثوقية المتجر ولا يضمن أن العرض صفقة حقيقية.',
  productDetailsUnavailable: 'تفاصيل المنتج غير متاحة',
  productDetailsLoading: 'جارٍ تحميل المنتج وعروض المتاجر…',
  productOffersCount: (n) => `${n} عرض`,
  productMarketCurrency: 'عملة السوق',
  productLastChecked: 'آخر تحقق',
  productMonitoring: 'المتابعة',
  productActiveListing: 'عرض نشط',
  productInactiveListing: 'عرض غير نشط',
  productIncludeOffer: 'ضم العرض للمتابعة',
  productExcludeOffer: 'استبعاد العرض من المتابعة',
  productOfferIncluded: (store) => `تم ضم عرض ${store} للمتابعة.`,
  productOfferExcluded: (store) => `تم استبعاد عرض ${store} من المتابعة.`,
  productExcluded: 'مستبعد',
  productIncluded: 'مضمّن',
  productNoStoredOffers: 'لا توجد عروض محفوظة',
  productNoStoredOffersBody: 'لا توجد حالياً قوائم متجر مرتبطة بهذا المنتج.',
  productHistoryPeriod7: '٧ أيام',
  productHistoryByStore: 'عرض سجل الأسعار حسب المتجر',
  productHistoryPartial: 'تعذّر تحميل بعض سجلات العروض؛ البيانات المعروضة جزئية.',
  productHistorySampled: 'نعرض نقاطاً تمثيلية من بداية الفترة ووسطها وأحدثها.',
  productHistoryChartLabel: (from, to, min, max) => `ملاحظات الأسعار من ${from} إلى ${to}. يتراوح السعر من ${min} إلى ${max}.`,
  productTrackingActivity: 'نشاط المتابعة',
  productTrackingStarted: 'بدأت المتابعة',
  productNavOffers: 'العروض',
  productNavHistory: 'سجل الأسعار',
  productNavActivity: 'نشاط المتابعة',
  productActivityEmpty: 'لا يوجد نشاط متابعة بعد.',
  productLowestVerified: 'أقل عرض تم التحقق منه يومياً',
  productHistoryEmpty: 'لا يتوفر سجل أسعار لهذه العروض.',
  productHistoryEmptyBody: 'ستظهر ملاحظات الأسعار بعد التحقق من هذه المتاجر.',
  productHistoryLoadFailed: 'تعذّر تحميل سجل الأسعار.',
  dashFeedEyebrow: 'من الكتالوج مباشرة',
  dashFeedHeading: 'اكتشافات جديدة',
  dashFeedWas: 'كان',
  dashFeedMovementDown: (percent, date) => `انخفض ${percent}% منذ ${date}`,
  dashFeedMovementUp: (percent, date) => `ارتفع ${percent}% منذ ${date}`,
  dashFeedStores: (n) => `${n} ${n === 1 ? 'متجر' : 'متاجر'}`,
  dashFeedLoading: 'جلب المزيد من الاكتشافات…',
  dashFeedMore: 'اكتشافات أخرى',
  dashFeedEnd: 'لقد شاهدت كل المنتجات المتاحة في هذه الجلسة.',
  dashFeedReshuffle: 'اخلط من جديد',
  dashFeedError: 'تعثّر التغذية أثناء التحميل.',
  dashFeedEmpty: 'لا اكتشافات بعد',
  dashFeedEmptyBody: 'لا يوجد شيء في الكتالوج حالياً. عد بعد أول كشط ناجح.',
  dashFeedOpen: (name) => `فتح ${name}`,
  productMovementDown: (percent, date) => `انخفض ${percent}% منذ ${date}`,
  productMovementUp: (percent, date) => `ارتفع ${percent}% منذ ${date}`,
  productOfferBest: 'أفضل سعر موثّق',
  productOfferVerified: 'موثّق',
  productOfferStale: 'قديم',
  productOfferUnverified: 'غير مؤكد',
  productOfferNoPrice: 'لا سعر',
  productOfferUnverifiedHint: 'لم يُرجع هذا العرض سعراً مؤكداً.',
  productOfferVerifiedHint: 'أعادت عملية سحب ناجحة هذا السعر الحالي.',
  productOfferStaleHint: 'لم يُعَد التحقق من هذا السعر مؤخراً.',
  productOfferNoPriceHint: 'لا توجد ملاحظة سعر ناجحة محفوظة لهذا العرض.',
  productPriceNotConfirmed: 'لا يوجد سعر حالي مؤكد',
  productPriceSource: (store) => `سعر موثّق لدى ${store}`,
  productImageMissing: 'لا تتوفر صورة للمنتج.',
  notFoundAriaLabel: 'الصفحة غير موجودة',
  notFoundStamp: 'انتهت الصفقة',
  notFoundEyebrow: 'العرض غير موجود',
  notFoundHeading: 'لم نعثر على المنتج أو العرض',
  notFoundDescription: 'ربما نُقلت الصفحة أو انتهى العرض.',
  notFoundObservation: 'الملاحظة',
  notFoundStatus: 'الحالة',
  notFoundStatusValue: 'غير متوفر',
  notFoundRecommendation: 'اقتراح',
  notFoundExploreDeals: 'استكشف أفضل العروض',
  notFoundDashboard: 'العودة إلى لوحة التحكم',
  skipToContent: 'تخطَّ إلى المحتوى',

  notificationsEyebrow: 'تنبيهاتك',
  notificationsHeading: 'الإشعارات',
  notificationsEmpty: 'لا توجد إشعارات بعد',
  notificationsEmptyBody: 'تابع منتجاً وحدد سعراً مستهدفاً لتلقّي التنبيهات.',
  notificationsMarkRead: 'وضع علامة كمقروء',
  notificationsMarkAllRead: 'وضع علامة مقروء للكل',
  notificationsFilterAll: 'الكل',
  notificationsFilterUnread: 'غير مقروء',
  notificationsFilterRead: 'مقروء',
  notificationsShowFilter: 'عرض',
  notificationsFilterType: 'النوع',
  notificationsFilterTarget: 'بلوغ السعر المستهدف',
  notificationsFilterFurther: 'انخفاض إضافي في السعر',
  notificationsFilterStopped: 'تم إيقاف التتبع',
  notificationsTargetReached: 'وصل السعر للهدف',
  notificationsFurtherDrop: 'انخفاض إضافي',
  notificationsTrackingStopped: 'تعذّر تأكيد سعر هذا العرض، لذلك تم إيقاف تتبعه.',
  notificationsAt: (price, target) => `${price} · الهدف ${target}`,
  notificationsFrom: (store) => `من ${store}`,
  notificationsEquivalent: 'ما يعادله محوّلاً',
  notificationsLoadError: 'تعذّر تحميل الإشعارات.',
  notificationsLoadErrorBody: 'لم تتغير إشعاراتك. حاول مرة أخرى.',

  settingsEyebrow: 'التفضيلات',
  settingsHeading: 'الإعدادات',
  settingsSubheading: '',
  settingsAccountHeading: 'الحساب',
  settingsAccountGuest: 'سجّل دخولك لعرض إعدادات الحساب.',
  guestPageAccessBody: 'سجّل دخولك لعرض المنتجات المحفوظة والإشعارات.',
  guestPageAccessHeading: 'سجّل دخولك للمتابعة',
  settingsAccountAccess: 'الوصول إلى الحساب',
  settingsAppearanceHeading: 'المظهر',
  settingsAppearanceSubheading: '',
  settingsTheme: 'السمة',
  settingsThemeLight: 'فاتح',
  settingsThemeDark: 'داكن',
  settingsThemeSystem: 'حسب النظام',
  settingsLanguage: 'اللغة',
  settingsLanguageEn: 'English',
  settingsLanguageAr: 'العربية',
  settingsMarketsHeading: 'الأسواق والعملات',
  settingsMarketsSubheading: '',
  settingsPreferredMarket: 'سوق التسوق المفضل',
  settingsDisplayCurrency: 'عملة العرض',
  settingsCurrencyMarket: 'استخدم عملة السوق المحدد',
  settingsChooseMarket: 'اختر سوقاً',
  settingsMarketEgypt: 'مصر · ج.م.',
  settingsMarketSaudi: 'المملكة العربية السعودية · ر.س.',
  settingsMarketUAE: 'الإمارات العربية المتحدة · د.إ.',
  settingsNotificationsHeading: 'الإشعارات',
  settingsNotificationsSubheading: '',
  settingsNotificationsPerProduct: 'التفضيلات لكل منتج',
  settingsTrackingHeading: 'البحث والمتابعة',
  settingsTrackingSubheading: '',
  settingsTrackingPaused: 'جميع مراقبة الأسعار موقوفة حالياً. استأنف لتلقّي تنبيهات جديدة.',
  settingsTrackingActive: 'المتابعة نشطة',
  settingsTrackingSignIn: 'سجّل دخولك للإدارة',
  settingsExportJson: 'تصدير JSON',
  settingsExportCsv: 'تصدير CSV',
  settingsExporting: 'جارٍ التصدير…',
  settingsDeleteHeading: 'حذف الحساب',
  settingsDeleteBody: '',
  settingsDeleteButton: 'طلب حذف الحساب',
  settingsCancelDeleteButton: 'إلغاء طلب الحذف',
  settingsDeleteConfirm: 'طلب حذف الحساب؟ سيُوقَف حسابك الآن ويُحذف نهائياً بعد فترة السماح.',
  settingsDeleteSuccess: 'تم طلب الحذف. أُوقفت المتابعة خلال فترة السماح.',
  settingsDeleteCancelled: 'تم إلغاء طلب حذف الحساب.',
  settingsDeletionScheduled: (date) => `موعد حذف الحساب ${date}. سجّل الدخول خلال فترة السماح لإلغاء الطلب.`,
  settingsAccountUnavailable: 'بيانات الحساب غير متاحة.',
  settingsSending: 'جارٍ الإرسال…',
  settingsSaving: 'جارٍ الحفظ…',
  settingsPauseAll: 'إيقاف جميع عمليات المتابعة',
  settingsResumeAll: 'استئناف جميع عمليات المتابعة',
  settingsExportComplete: 'تم تنزيل نسخة بيانات الحساب.',
  settingsEmailVerify: 'عنوان البريد الإلكتروني',
  settingsEmailVerified: 'موثّق',
  settingsEmailUnverified: 'غير موثّق — تنبيهات البريد معطّلة',
  settingsSendVerification: 'إرسال بريد التحقق',
  settingsVerificationSent: 'تم إرسال بريد التحقق.',
  settingsMatchReviewHeading: 'مراجعة مطابقة المنتجات',
  settingsMatchReviewBody: '',
  settingsMatchReviewEmpty: 'لا توجد مطابقات منتجات للمراجعة.',
  settingsMatchReviewSource: 'المنتج المتابَع',
  settingsMatchReviewCandidate: 'المنتج المرشح الموثّق',
  settingsMatchReviewAccept: 'قبول المطابقة',
  settingsMatchReviewDismiss: 'تجاهل',
  settingsMatchReviewSaved: 'تم حسم مراجعة المطابقة.',
  settingsMatchReviewLoadError: 'تعذّر تحميل مراجعات تطابق المنتجات. حاول مرة أخرى.',
  settingsOpsHeading: 'نظرة عامة على العمليات',
  settingsOpsBody: '',
  settingsOpsActiveUsers: 'الحسابات النشطة',
  settingsOpsActiveTrackings: 'قواعد المتابعة النشطة',
  settingsOpsVerifiedOffers: 'العروض النشطة',
  settingsOpsActiveStores: 'المتاجر النشطة',
  settingsOpsPendingOutbox: 'الرسائل غير المنشورة',
  settingsOpsScrapeJobs: 'عمليات جمع الأسعار خلال آخر 24 ساعة',
  settingsOpsStoreHealth: 'صحة جمع الأسعار حسب المتجر',
  settingsOpsNoStoreHealth: 'لا توجد متاجر مهيأة.',

  previewEyebrow: 'معاينة المنتج',
  previewHeading: 'راجع المنتج قبل بدء متابعته',
  previewListedIn: (currency) => `مُدرج بـ ${currency}`,
  previewPriceLabel: 'سعرك المستهدف',
  previewPriceLabel2: (currency) => `سعرك المستهدف (${currency})`,
  previewEmailPreferences: 'تفضيلات البريد الإلكتروني',
  previewEmailTarget: 'أرسل لي بريداً حين يصل السعر للهدف',
  previewEmailFurther: 'أرسل لي بريداً عند انخفاضات لاحقة',
  previewHelp: 'سيُحفظ المنتج بعد التأكيد. المبلغ المعروض لا يُعدّ قراءة سعر موثّقة.',
  previewCancel: 'إلغاء',
  previewConfirm: 'تأكيد المتابعة',
  previewSaving: 'جارٍ الحفظ…',
  previewSignInToTrack: 'سجّل دخولك للمتابعة',
  previewTrackingStarted: 'بدأت المتابعة. تم حفظ هدفك.',
  previewDecreaseTarget: 'خفض السعر المستهدف بمقدار واحد',
  previewIncreaseTarget: 'رفع السعر المستهدف بمقدار واحد',
  previewVerificationTimeout: 'استغرق تحميل تفاصيل المنتج وقتاً أطول من المتوقع. جرّب الرابط مرة أخرى.',
  previewDetailsFailed: 'تعذّر قراءة صفحة المنتج.',
  previewDataUnreadable: 'تعذّرت قراءة تفاصيل المنتج.',
  previewStreamInterrupted: 'توقف تحميل تفاصيل المنتج قبل اكتماله. حاول مرة أخرى.',
  previewInvalidPrice: 'أدخل سعراً مستهدفاً أكبر من صفر.',
  previewInvalidCurrency: 'تعذّر تحديد عملة المتجر. لا يمكن متابعة هذا العرض حالياً.',
  previewNoUrl: 'هذه النتيجة لا تحتوي على رابط منتج مدعوم للمتابعة.',
  previewNoOffer: 'لا يوجد عرض نشط بعملة المصدر المطابقة للمتابعة.',

  authEyebrow: 'ديليو',
  authLoginTitle: 'أهلاً بعودتك',
  authRegisterTitle: 'إنشاء حسابك',
  authNameLabel: 'الاسم',
  authEmailLabel: 'البريد الإلكتروني',
  authPasswordLabel: 'كلمة المرور',
  authLoginButton: 'تسجيل الدخول',
  authRegisterButton: 'إنشاء حساب',
  authWorking: 'يُرجى الانتظار…',
  authGoogleButton: 'المتابعة باستخدام Google',
  authForgotPassword: 'نسيت كلمة المرور؟',
  authNewUser: 'جديد على Dealio؟',
  authExistingUser: 'لديك حساب بالفعل؟',
  authCreateAccount: 'إنشاء حساب',
  authSignIn: 'تسجيل الدخول',
  authHelp: 'يمكنك البدء في المتابعة بعد التسجيل. التحقق من البريد مطلوب لتنبيهات البريد الإلكتروني.',
  authRegisterSuccess: 'تم إنشاء الحساب. يمكنك بدء المتابعة الآن؛ تحقّق من بريدك لتلقي التنبيهات.',
  authGoogleFailure: 'تعذّر تسجيل الدخول باستخدام Google. حاول مرة أخرى.',
  authGoogleIncomplete: 'لم يكتمل تسجيل الدخول باستخدام Google. حاول مرة أخرى.',
  authGoogleUnavailable: 'تسجيل الدخول باستخدام Google غير مُعدّ لهذا التطبيق بعد.',
  authOr: 'أو',
  authSendResetLink: 'إرسال رابط إعادة التعيين',

  authVerifyTitle: 'تحقق من بريدك الإلكتروني',
  authVerifyBody: 'تم التحقق من بريدك. يمكن الآن إرسال تنبيهات البريد الإلكتروني.',
  authVerifyFailed: 'رابط التحقق غير صالح أو انتهت صلاحيته.',
  authResetTitle: 'اختر كلمة مرور جديدة',
  authResetSaved: 'تم تغيير كلمة مرورك.',
  authResetContinue: 'المتابعة لتسجيل الدخول',
  authResetSaving: 'جارٍ الحفظ…',
  authResetNewPassword: 'كلمة المرور الجديدة',
  authForgotTitle: 'إعادة تعيين كلمة المرور',
  authForgotSent: 'إذا كان هذا العنوان مرتبطاً بحساب، تم إرسال رابط إعادة التعيين.',
  authForgotBack: 'العودة إلى Dealio',
  authContinue: 'المتابعة إلى Dealio',
  authPasswordResetConfirm: 'رابط إعادة تعيين كلمة المرور لا يحتوي على رمز.',
  authMissingToken: 'رابط التحقق لا يحتوي على رمز.',

  loading: 'جارٍ التحميل…',
  loadingProducts: 'جارٍ تحميل المنتجات المتابَعة',
  tryAgain: 'حاول مجدداً',
  close: 'إغلاق',
  priceHistory: 'تاريخ السعر',
  convertedRate: (date) => `سعر الصرف بتاريخ ${date}`,
  convertedOriginal: (price) => `السعر الأصلي ${price}`,
  conversionRates: 'الأسعار',
  mobileNavClose: 'إغلاق التنقل',
};

export const translations: Record<Locale, Strings> = { en, ar };

