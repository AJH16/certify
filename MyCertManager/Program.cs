using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Vault.Providers;
using System.IO;
using Certify;
using Certify.Management;
using Certify.Models;

namespace MyCertManager
{
	class Program
	{
		static void Main(string[] args)
		{
			List<string> domainNames = new List<string>();
			StreamReader reader = new StreamReader("domainList.txt");
			while(!reader.EndOfStream)
			{
				domainNames.Add(reader.ReadLine());
			}

			VaultManager vault = new VaultManager("c:\\certs\\letsencryptvault\\", LocalDiskVault.VAULT);
			vault.InitVault(staging: true);
			if (!vault.HasContacts(loadConfig: true))
			{
				vault.AddNewRegistrationAndAcceptTOS("mailto:ajh16@nycap.rr.com");
			}

			Dictionary<string, string> aliases = new Dictionary<string, string>();

			foreach (string name in domainNames)
			{
				aliases.Add(name, vault.ComputeIdentifierAlias(name));
			}

			CertRequestConfig requestConfig = new CertRequestConfig();
			requestConfig.PrimaryDomain = "ajhenderson.com";
			requestConfig.ChallengeType = "dns-01";
			requestConfig.SubjectAlternativeNames = domainNames.ToArray();

			bool validated = true;
			foreach(KeyValuePair<string,string> alias in aliases)
			{
				PendingAuthorization auth = vault.BeginDNSRegistrationAndValidation(requestConfig, alias.Value, challengeType: "dns-01", domain: alias.Key);
				if (auth.Identifier.Authorization.IsPending())
				{
					vault.SubmitChallenge(alias.Value, challengeType: "dns-01");
				}
				validated = validated && vault.CompleteIdentifierValidationProcess(alias.Value);
			}
			IISManager iisManager = new IISManager();
			if (validated)
			{				
				var certRequestResult = vault.PerformCertificateRequestProcess(aliases.Values.First(), alternativeIdentifierRefs: aliases.Values.ToArray());
				if (certRequestResult.IsSuccess)
				{
					string pfxPath = certRequestResult.Result.ToString();
					iisManager.StoreCertificate(aliases.Keys.First(), pfxPath);
				}
			}
			else
			{
				throw new InvalidOperationException("Validation failed.");
			}
		}
	}
}
