"""Make the test suite runnable from anywhere (repo root included).

pytest runs with the invocation directory's context, so `pytest scraper/tests`
from the repository root cannot import `dealio_scraper` by itself.
Point sys.path at the scraper project root (which is this directory's
parent) before any test module is collected.
"""

from __future__ import annotations

import sys
from pathlib import Path

_SCRAPER_ROOT = Path(__file__).resolve().parent.parent
if str(_SCRAPER_ROOT) not in sys.path:
    sys.path.insert(0, str(_SCRAPER_ROOT))
