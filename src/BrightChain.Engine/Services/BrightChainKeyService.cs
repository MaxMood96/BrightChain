using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BrightChain.Engine.Models.Blocks;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Sec;

namespace BrightChain.Engine.Services
{
    /// <summary>
    /// Loads keys from blocks, stores keys to blocks.
    /// </summary>
    public static class BrightChainKeyService
    {
        public static ECDsa LoadPrivateKeyFromBlock(BlockCacheManager blockCacheManager, BlockHash id)
        {
            var brightChainKeyBlock = blockCacheManager.Get(id);
            // get data from block
            throw new NotImplementedException();
        }

        public static ECDsa LoadPrivateKey(string hexKeyString)
        {
            return LoadPrivateKey(FromHexString(hexKeyString));
        }

        public static ECDsa LoadPrivateKey(byte[] key)
        {
            var privKeyInt = new Org.BouncyCastle.Math.BigInteger(+1, key);
            var parameters = SecNamedCurves.GetByName("secp256r1");
            var ecPoint = parameters.G.Multiply(privKeyInt);
            var privKeyX = ecPoint.Normalize().XCoord.ToBigInteger().ToByteArrayUnsigned();
            var privKeyY = ecPoint.Normalize().YCoord.ToBigInteger().ToByteArrayUnsigned();

            return ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = privKeyInt.ToByteArrayUnsigned(),
                Q = new ECPoint
                {
                    X = privKeyX,
                    Y = privKeyY,
                },
            });
        }

        public static ECDsa LoadPublicKey(string hexKeyString)
        {
            return LoadPublicKey(FromHexString(hexKeyString));
        }

        public static ECDsa LoadPublicKey(byte[] key)
        {
            var pubKeyX = key.Skip(1).Take(32).ToArray();
            var pubKeyY = key.Skip(33).ToArray();

            return ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = pubKeyX,
                    Y = pubKeyY,
                },
            });
        }

        public static string CreateSignedJwt(ECDsa eCDsa)
        {
            var now = DateTime.UtcNow;
            var tokenHandler = new JwtSecurityTokenHandler();

            var jwtToken = tokenHandler.CreateJwtSecurityToken(
                issuer: "BrightChain",
                audience: "you",
                subject: null,
                notBefore: now,
                expires: now.AddMinutes(30),
                issuedAt: now,
                signingCredentials: new SigningCredentials(
                    key: new ECDsaSecurityKey(eCDsa),
                    algorithm: SecurityAlgorithms.EcdsaSha256));

            return tokenHandler.WriteToken(jwtToken);
        }

        public static bool VerifySignedJwt(ECDsa eCDsa, string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var claimsPrincipal = tokenHandler.ValidateToken(
                token: token,
                validationParameters: new TokenValidationParameters
                {
                    ValidIssuer = "BrightChain",
                    ValidAudience = "you",
                    IssuerSigningKey = new ECDsaSecurityKey(eCDsa),
                },
                validatedToken: out var parsedToken);

            return claimsPrincipal.Identity.IsAuthenticated;
        }

        private static byte[] FromHexString(string hex)
        {
            var numberChars = hex.Length;
            var hexAsBytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
            {
                hexAsBytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return hexAsBytes;
        }
    }
}
