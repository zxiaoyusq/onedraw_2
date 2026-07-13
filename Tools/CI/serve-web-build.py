#!/usr/bin/env python3
"""Serve a Unity WebGL build locally with correct Brotli response headers."""

from __future__ import annotations

import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class UnityWebHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path: str) -> str:
        if path.endswith(".wasm.br"):
            return "application/wasm"
        if path.endswith(".js.br"):
            return "application/javascript"
        if path.endswith(".data.br"):
            return "application/octet-stream"
        return super().guess_type(path)

    def end_headers(self) -> None:
        if self.path.split("?", 1)[0].endswith(".br"):
            self.send_header("Content-Encoding", "br")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path("Builds/WebGL"))
    parser.add_argument("--bind", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8123)
    args = parser.parse_args()

    root = args.root.resolve()
    if not (root / "index.html").is_file():
        parser.error(f"Unity WebGL index.html not found under {root}")

    handler = partial(UnityWebHandler, directory=str(root))
    server = ThreadingHTTPServer((args.bind, args.port), handler)
    print(f"UNITY_WEB_SERVER http://{args.bind}:{args.port}/ root={root}", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
