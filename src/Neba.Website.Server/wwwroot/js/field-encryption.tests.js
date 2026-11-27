/**
 * @jest-environment node
 */
// Tests for field-encryption.js. They check the wire layout of a committed sample (which a C# test
// decrypts) and of fresh output, so a format change on either side fails the build.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { encryptField } from './field-encryption.js';

const sample = JSON.parse(readFileSync(
    resolve(process.cwd(), 'tests/Neba.Api.Tests/Encryption/Samples/browser-sample.json'), 'utf8'));

// The throwaway key committed for Development, which made the sample readable. The settings file has
// comments, so it is not plain JSON.
const developmentPrivateKey = readFileSync(
    resolve(process.cwd(), 'src/Neba.Api/appsettings.Development.json'), 'utf8')
    .match(/"Encryption:Keys:BowlerPii:LocalPrivateKey": "([^"]+)"/)[1];

const encoder = new TextEncoder();
const decoder = new TextDecoder();

function parse(wireValue) {
    const bytes = Buffer.from(wireValue, 'base64');
    let offset = 0;
    const take = (length) => {
        const part = bytes.subarray(offset, offset + length);
        expect(part.length).toBe(length);
        offset += length;
        return part;
    };

    const formatVersion = take(1)[0];
    const keyVersion = take(take(1)[0]).toString('utf8');
    const wrappedKey = take(take(2).readUInt16BE(0));
    const nonce = take(12);
    const tag = take(16);
    const ciphertext = bytes.subarray(offset);

    return { formatVersion, keyVersion, wrappedKey, nonce, tag, ciphertext };
}

function expectLayout(wireValue, keyVersion, plaintext) {
    const parts = parse(wireValue);
    expect(parts.formatVersion).toBe(1);
    expect(parts.keyVersion).toBe(keyVersion);
    expect(parts.wrappedKey).toHaveLength(256);
    expect(parts.ciphertext).toHaveLength(encoder.encode(plaintext).length);
}

async function createKeyPair() {
    const pair = await crypto.subtle.generateKey(
        { name: 'RSA-OAEP', modulusLength: 2048, publicExponent: Uint8Array.of(1, 0, 1), hash: 'SHA-256' },
        false, ['encrypt', 'decrypt']);
    const spki = await crypto.subtle.exportKey('spki', pair.publicKey);

    return { publicKey: Buffer.from(spki).toString('base64'), privateKey: pair.privateKey };
}

// Decrypts the way the API does, using only the documented format.
async function decrypt(wireValue, privateKey, keyName) {
    const { keyVersion, wrappedKey, nonce, tag, ciphertext } = parse(wireValue);
    const aesKeyBytes = await crypto.subtle.decrypt({ name: 'RSA-OAEP' }, privateKey, wrappedKey);
    expect(aesKeyBytes.byteLength).toBe(32);
    const aesKey = await crypto.subtle.importKey('raw', aesKeyBytes, { name: 'AES-GCM' }, false, ['decrypt']);
    const additionalData = encoder.encode(`v1|${keyName}|wire|${keyVersion}`);
    const plaintext = await crypto.subtle.decrypt(
        { name: 'AES-GCM', iv: nonce, additionalData }, aesKey, Buffer.concat([ciphertext, tag]));

    return decoder.decode(plaintext);
}

describe('field-encryption', () => {
    const value = '900123456';
    let keys;

    beforeAll(async () => {
        keys = await createKeyPair();
    });

    test('the committed sample has the wire layout', () => {
        expectLayout(sample.wireValue, sample.keyVersion, sample.plaintext);
    });

    // A format change made in the module and in decrypt() above fails here until the sample is
    // remade, and the C# test then checks the new sample.
    test('the committed sample decrypts with the development key', async () => {
        const privateKey = await crypto.subtle.importKey(
            'pkcs8', Buffer.from(developmentPrivateKey, 'base64'), { name: 'RSA-OAEP', hash: 'SHA-256' },
            false, ['decrypt']);

        await expect(decrypt(sample.wireValue, privateKey, sample.keyName)).resolves.toBe(sample.plaintext);
    });

    test('fresh output has the wire layout', async () => {
        const wireValue = await encryptField(value, keys.publicKey, 'BowlerPii', 'abc123');

        expectLayout(wireValue, 'abc123', value);
        expect(Buffer.from(wireValue, 'base64')).toHaveLength(2 + 6 + 2 + 256 + 12 + 16 + value.length);
    });

    test('fresh output decrypts with the private key under the wire purpose', async () => {
        const wireValue = await encryptField(value, keys.publicKey, 'BowlerPii', 'abc123');

        await expect(decrypt(wireValue, keys.privateKey, 'BowlerPii')).resolves.toBe(value);
    });

    test('output is bound to the key name', async () => {
        const wireValue = await encryptField(value, keys.publicKey, 'BowlerPii', 'abc123');

        await expect(decrypt(wireValue, keys.privateKey, 'OtherPii')).rejects.toThrow();
    });

    test('the same input gives different output each time', async () => {
        const first = await encryptField(value, keys.publicKey, 'BowlerPii', 'local');
        const second = await encryptField(value, keys.publicKey, 'BowlerPii', 'local');

        expect(first).not.toBe(second);
    });

    test('a large value does not overflow the call stack', async () => {
        const large = 'a'.repeat(300000);

        const wireValue = await encryptField(large, keys.publicKey, 'BowlerPii', 'local');

        expectLayout(wireValue, 'local', large);
    });

    test('the AES key is cleared when encryption fails', async () => {
        const encrypt = jest.spyOn(crypto.subtle, 'encrypt').mockRejectedValue(new Error('encrypt failed'));
        const fill = jest.spyOn(Uint8Array.prototype, 'fill');

        await expect(encryptField(value, keys.publicKey, 'BowlerPii', 'local')).rejects.toThrow('encrypt failed');

        expect(fill).toHaveBeenCalledWith(0);
        encrypt.mockRestore();
        fill.mockRestore();
    });

    test('a key version over 255 bytes is rejected', async () => {
        await expect(encryptField(value, keys.publicKey, 'BowlerPii', 'v'.repeat(256)))
            .rejects.toThrow('key version');
    });
});
