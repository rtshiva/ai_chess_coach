/**
 * Utilities for processing and sanitizing PGN strings.
 */
export function processPgn(pgnString) {
    if (!pgnString) return '';

    let sanitized = pgnString;

    // Remove CR characters
    sanitized = sanitized.replace(/\r/g, '');

    // Remove PGN headers
    sanitized = sanitized.replace(/\[[^\]]*]/g, '');

    // Remove comments { ... }
    sanitized = sanitized.replace(/\{[^}]*}/g, '');

    // Remove variations ( ... )
    sanitized = sanitized.replace(/\([^)]*\)/g, '');

    // Normalize move numbers:
    // 1.d4 -> 1. d4
    sanitized = sanitized.replace(/(\d+)\.(\S)/g, '$1. $2');

    // Normalize black move notation:
    // 1...Nf6 -> 1... Nf6
    sanitized = sanitized.replace(/(\d+)\.\.\.(\S)/g, '$1... $2');

    // Find first move
    const moveStartMatch = sanitized.match(/\d+\./);
    if (moveStartMatch) {
        sanitized = sanitized.substring(
            sanitized.indexOf(moveStartMatch[0])
        );
    }

    // Normalize whitespace
    sanitized = sanitized.replace(/\s+/g, ' ').trim();

    // Remove game result before loading for better compatibility with chess libraries
    sanitized = sanitized.replace(/\s(1-0|0-1|1\/2-1\/2|\*)\s*$/g, '');

    return sanitized;
}
