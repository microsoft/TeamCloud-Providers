/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

/**
 *  MIT License
 *
 *  Copyright (c) 2018 Adrian Godong
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Jose;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;

namespace TeamCloud.Providers.GitHub.Actions.Services
{
    public interface IGitHubJwtFactory
    {
        string CreateEncodedJwtToken();
    }

    public class GitHubJwtFactory : IGitHubJwtFactory
    {
        private static readonly long TicksSince197011 = new DateTime(1970, 1, 1).Ticks;
        private readonly IPrivateKeySource privateKeySource;
        private readonly GitHubJwtFactoryOptions options;

        public GitHubJwtFactory(IPrivateKeySource privateKeySource, GitHubJwtFactoryOptions options)
        {
            this.privateKeySource = privateKeySource;
            this.options = options;
        }

        public string CreateEncodedJwtToken()
        {
            var utcNow = DateTime.UtcNow;

            var payload = new Dictionary<string, object>
            {
                {"iat", ToUtcSeconds(utcNow)},
                {"exp", ToUtcSeconds(utcNow.AddSeconds(options.ExpirationSeconds))},
                {"iss", options.AppIntegrationId}
            };

            // Generate JWT
            using var rsa = new RSACryptoServiceProvider();
            var rsaParams = ToRSAParameters(GetPrivateKey());
            rsa.ImportParameters(rsaParams);
            return JWT.Encode(payload, rsa, JwsAlgorithm.RS256);
        }

        private RsaPrivateCrtKeyParameters GetPrivateKey()
        {
            using var privateKeyReader = privateKeySource.GetPrivateKeyReader();
            var pemReader = new PemReader(privateKeyReader);
            var keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
            return (RsaPrivateCrtKeyParameters)keyPair.Private;
        }

        private static RSAParameters ToRSAParameters(RsaPrivateCrtKeyParameters privKey)
        {
            var rp = new RSAParameters
            {
                Modulus = privKey.Modulus.ToByteArrayUnsigned(),
                Exponent = privKey.PublicExponent.ToByteArrayUnsigned(),
                P = privKey.P.ToByteArrayUnsigned(),
                Q = privKey.Q.ToByteArrayUnsigned()
            };
            rp.D = ConvertRSAParametersField(privKey.Exponent, rp.Modulus.Length);
            rp.DP = ConvertRSAParametersField(privKey.DP, rp.P.Length);
            rp.DQ = ConvertRSAParametersField(privKey.DQ, rp.Q.Length);
            rp.InverseQ = ConvertRSAParametersField(privKey.QInv, rp.Q.Length);
            return rp;
        }

        private static byte[] ConvertRSAParametersField(BigInteger n, int size)
        {
            byte[] bs = n.ToByteArrayUnsigned();

            if (bs.Length == size)
                return bs;

            if (bs.Length > size)
                throw new ArgumentException("Specified size too small", nameof(size));

            byte[] padded = new byte[size];
            Array.Copy(bs, 0, padded, size - bs.Length, bs.Length);
            return padded;
        }

        private static long ToUtcSeconds(DateTime dt)
        {
            return (dt.ToUniversalTime().Ticks - TicksSince197011) / TimeSpan.TicksPerSecond;
        }
    }

    public interface IPrivateKeySource
    {
        TextReader GetPrivateKeyReader();
    }

    public class StringPrivateKeySource : IPrivateKeySource
    {
        protected readonly string Key;

        public StringPrivateKeySource(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
        }

        public TextReader GetPrivateKeyReader()
        {
            return new StringReader(Key/*.HydrateRsaVariable()*/);
        }
    }

    public class FilePrivateKeySource : IPrivateKeySource
    {
        private readonly string filePath;

        public FilePrivateKeySource(string filePath)
        {
            this.filePath = filePath;
        }

        public TextReader GetPrivateKeyReader()
        {
            return new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        }
    }

    public class GitHubJwtFactoryOptions
    {
        public int AppIntegrationId { get; set; }
        public int ExpirationSeconds { get; set; }
    }
}
