from __future__ import annotations

import sys


def force_utf8_stdio() -> None:
    """Switch stdout and stderr to UTF-8, whatever the console code page is.

    The validation scripts report localized README titles and connector names.
    Python encodes standard output with the locale code page, so on a Windows
    machine outside a Cyrillic or Chinese locale - the GitHub runner included -
    printing such a name raises UnicodeEncodeError and fails the whole check.
    """
    # stderr keeps the forgiving handler Python gives it, so reporting a failure
    # can never fail on its own output.
    for stream, errors in ((sys.stdout, "strict"), (sys.stderr, "backslashreplace")):
        reconfigure = getattr(stream, "reconfigure", None)
        if reconfigure is not None:
            reconfigure(encoding="utf-8", errors=errors)
