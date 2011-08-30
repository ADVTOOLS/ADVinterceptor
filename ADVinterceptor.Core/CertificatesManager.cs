/*
 * This file is part of ADVinterceptor
 * Copyright (c) 2011 - ADVTOOLS SARL
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using Mono.Security.Authenticode;
using Mono.Security.X509;
using Mono.Security.X509.Extensions;
using MSX509 = System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace Advtools.AdvInterceptor
{
    internal class CertificatesManager
    {
        private const MSX509.StoreLocation location_ = MSX509.StoreLocation.CurrentUser;
        private readonly State state_;
        private Dictionary<string, MSX509.X509Certificate2> certificates_ = new Dictionary<string,MSX509.X509Certificate2>();

        public CertificatesManager(State state)
        {
            state_ = state;
        }

        internal MSX509.X509Certificate2 GetCertificate(string name)
        {
            List<X509Extension> extensions = new List<X509Extension>();

            BasicConstraintsExtension constraints = new BasicConstraintsExtension();
            constraints.CertificateAuthority = false;
            constraints.Critical = true;
            extensions.Add(constraints);

            KeyUsageExtension keyUsage = new KeyUsageExtension();
            keyUsage.KeyUsage = KeyUsages.digitalSignature | KeyUsages.nonRepudiation | KeyUsages.keyEncipherment;
            extensions.Add(keyUsage);

            ExtendedKeyUsageExtension extendedUsage = new ExtendedKeyUsageExtension();
            extendedUsage.KeyPurpose.Add("1.3.6.1.5.5.7.3.1");
            extendedUsage.KeyPurpose.Add("1.3.6.1.5.5.7.3.2");
            extensions.Add(extendedUsage);

            return CreateCertificate(name, extensions, GetRootCertificate(), state_.Config.X509.AuthorityName, MSX509.StoreName.My, state_.Config.X509.RootValidity);
        }

        private MSX509.X509Certificate2 CreateCertificate(string name, List<X509Extension> extensions, MSX509.X509Certificate2 issuerCertificate, string issuer, MSX509.StoreName storeName, int validity)
        {
            MSX509.X509Certificate2 certificate = LoadCertificate(name, storeName, location_);
            if(certificate != null)
                return certificate;

            state_.Logger.Information("Create X509.v3 certificate for '{0}'", name);
            
            PrivateKey key = new PrivateKey();
            key.RSA = RSA.Create();

            X509CertificateBuilder builder = new X509CertificateBuilder(3);
            builder.SerialNumber = GenerateSerial();
            builder.IssuerName = "CN=" + issuer;
            builder.SubjectName = "CN=" + name;
            builder.SubjectPublicKey = key.RSA;
            builder.NotBefore = DateTime.Now;
            builder.NotAfter = builder.NotBefore.AddDays(validity);
            builder.Hash = "SHA1";

            foreach(X509Extension extension in extensions)
                builder.Extensions.Add(extension);

            var signator = issuerCertificate == null ? key.RSA : issuerCertificate.PrivateKey;
            byte[] raw = builder.Sign(signator);

            StoreCertificate(name, raw, key.RSA, storeName, location_);

            certificate = new MSX509.X509Certificate2(raw);
            certificate.PrivateKey = key.RSA;
            return certificate;

        }

        private MSX509.X509Certificate2 GetRootCertificate()
        {
            List<X509Extension> extensions = new List<X509Extension>();

            BasicConstraintsExtension constraints = new BasicConstraintsExtension();
            constraints.CertificateAuthority = true;
            constraints.Critical = true;
            extensions.Add(constraints);

            KeyUsageExtension keyUsage = new KeyUsageExtension();
            keyUsage.KeyUsage = KeyUsages.keyCertSign | KeyUsages.cRLSign;
            extensions.Add(keyUsage);

            return CreateCertificate(state_.Config.X509.AuthorityName, extensions, null, state_.Config.X509.AuthorityName, MSX509.StoreName.Root, state_.Config.X509.RootValidity);
        }

        private byte[] GenerateSerial()
        {
            byte[] serial = Guid.NewGuid().ToByteArray();
            if((serial[0] & 0x80) == 0x80) // Have to be positive
                serial[0] -= 0x80;
            return serial;
        }

        private PKCS12 BuildPkcs12(byte[] raw, RSA key)
        {
            PKCS12 p12 = new PKCS12();
            p12.Password = "advtools";

            ArrayList list = new ArrayList();
            // we use a fixed array to avoid endianess issues (in case some tools requires the ID to be 1).
            list.Add(new byte[4] { 1, 0, 0, 0 });
            Hashtable attributes = new Hashtable(1);
            attributes.Add(PKCS9.localKeyId, list);

            p12.AddCertificate(new X509Certificate(raw), attributes);
            p12.AddPkcs8ShroudedKeyBag(key, attributes);

            return p12;
        }

        private void StoreCertificate(string name, byte[] raw, RSA key, MSX509.StoreName storeName, MSX509.StoreLocation location)
        {
            PKCS12 p12 = BuildPkcs12(raw, key);

            MSX509.X509Certificate2 certificate = new MSX509.X509Certificate2(p12.GetBytes(), "advtools", MSX509.X509KeyStorageFlags.PersistKeySet | MSX509.X509KeyStorageFlags.MachineKeySet | MSX509.X509KeyStorageFlags.Exportable);

            MSX509.X509Store store = new MSX509.X509Store(storeName, location);
            store.Open(MSX509.OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();

            certificates_[name] = certificate;
        }

        /*private void WriteCertificate(string name, byte[] raw)
        {
            using(FileStream stream = new FileStream(GetCertificateFilePath(name), FileMode.Create, FileAccess.Write))
                stream.Write(raw, 0, raw.Length);
        }*/

        private MSX509.X509Certificate2 LoadCertificate(string name, MSX509.StoreName storeName, MSX509.StoreLocation location)
        {
            if(certificates_.ContainsKey(name))
                return certificates_[name];

            MSX509.X509Store store = new MSX509.X509Store(storeName, location);
            store.Open(MSX509.OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(MSX509.X509FindType.FindBySubjectName, name, true);
            store.Close();

            if(certificates.Count <= 0)
                return null;

            state_.Logger.Information("X509v3 '{0}' loaded from store", name);

            MSX509.X509Certificate2 certificate = certificates[0];
            certificates_[name] = certificate;
            return certificate;
        }
    }
}
