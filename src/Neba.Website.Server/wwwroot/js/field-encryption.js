// Encrypts one form value in the browser, so its plaintext never reaches the web server.
// Uses WebCrypto only. The API decrypts the result (src/Neba.Api/Encryption/Envelope.cs holds the
// format; keep the two in step):
//
//   Base64 of: format version (1 byte, value 1) | key version length (1) | key version (UTF-8)
//     | wrapped key length (2, big-endian) | wrapped key | nonce (12) | tag (16) | ciphertext
//
// A new AES-256-GCM key and nonce per value; the AES key is wrapped with RSA-OAEP (SHA-256).
// The format does not prove who wrote a value: the public key is public.

const FORMAT_VERSION = 1;
const NONCE_SIZE = 12;
const TAG_SIZE = 16;

function fromBase64(text) {
    return Uint8Array.from(atob(text), (character) => character.charCodeAt(0));
}

// Byte by byte: spreading a large array into String.fromCharCode overflows the call stack.
function toBase64(bytes) {
    return btoa(Array.from(bytes, (byte) => String.fromCharCode(byte)).join(''));
}

function concat(parts) {
    const result = new Uint8Array(parts.reduce((total, part) => total + part.length, 0));
    let offset = 0;
    for (const part of parts) {
        result.set(part, offset);
        offset += part.length;
    }
    return result;
}

/**
 * @param {string} value The plaintext.
 * @param {string} publicKey The RSA public key: Base64 SPKI.
 * @param {string} keyName The key name, for example "BowlerPii".
 * @param {string} keyVersion The version of the public key.
 * @returns {Promise<string>} The wire value.
 */
export async function encryptField(value, publicKey, keyName, keyVersion) {
    const { subtle } = globalThis.crypto;
    const encoder = new TextEncoder();

    const versionBytes = encoder.encode(keyVersion);
    if (versionBytes.length > 255) {
        throw new Error('The key version is too long.');
    }

    const rsaKey = await subtle.importKey(
        'spki', fromBase64(publicKey), { name: 'RSA-OAEP', hash: 'SHA-256' }, false, ['encrypt']);

    const aesKeyBytes = globalThis.crypto.getRandomValues(new Uint8Array(32));
    try {
        const aesKey = await subtle.importKey('raw', aesKeyBytes, { name: 'AES-GCM' }, false, ['encrypt']);
        const nonce = globalThis.crypto.getRandomValues(new Uint8Array(NONCE_SIZE));
        const additionalData = encoder.encode(`v1|${keyName}|wire|${keyVersion}`);

        // WebCrypto returns the tag appended to the ciphertext; the format stores it first.
        const sealed = new Uint8Array(await subtle.encrypt(
            { name: 'AES-GCM', iv: nonce, additionalData, tagLength: TAG_SIZE * 8 }, aesKey, encoder.encode(value)));
        const ciphertext = sealed.subarray(0, sealed.length - TAG_SIZE);
        const tag = sealed.subarray(sealed.length - TAG_SIZE);

        const wrappedKey = new Uint8Array(await subtle.encrypt({ name: 'RSA-OAEP' }, rsaKey, aesKeyBytes));

        return toBase64(concat([
            Uint8Array.of(FORMAT_VERSION, versionBytes.length),
            versionBytes,
            Uint8Array.of(wrappedKey.length >> 8, wrappedKey.length & 0xff),
            wrappedKey,
            nonce,
            tag,
            ciphertext,
        ]));
    } finally {
        aesKeyBytes.fill(0);
    }
}
