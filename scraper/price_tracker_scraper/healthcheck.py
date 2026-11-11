from __future__ import annotations

import os
import socket
import sys


def main() -> int:
    host = os.getenv("RABBITMQ_HOST", "rabbitmq")
    port = int(os.getenv("RABBITMQ_PORT", "5672"))
    try:
        with socket.create_connection((host, port), timeout=2):
            return 0
    except OSError:
        return 1


if __name__ == "__main__":
    sys.exit(main())
