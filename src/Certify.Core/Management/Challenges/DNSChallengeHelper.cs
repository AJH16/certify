﻿using Certify.Management;
using Certify.Models;
using Certify.Models.Config;
using Certify.Models.Providers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Certify.Core.Management.Challenges
{
    public class DNSChallengeHelper
    {
        public async Task<ActionResult> CompleteDNSChallenge(ManagedSite managedsite, string domain, string txtRecordName, string txtRecordValue)
        {
            // for a given managed site configuration, attempt to complete the required challenge by
            // creating the required TXT record

            var credentialsManager = new CredentialsManager();
            string[] credentialArray;
            string zoneId = null;

            Models.Config.ProviderDefinition providerDefinition;
            IDnsProvider dnsAPIProvider = null;

            if (!String.IsNullOrEmpty(managedsite.RequestConfig.ChallengeCredentialKey))
            {
                // decode credentials string array
                credentialArray = await credentialsManager.GetUnlockedCredentialsArray(managedsite.RequestConfig.ChallengeCredentialKey);
            }
            else
            {
                return new ActionResult { IsSuccess = false, Message = "DNS Challenge API Credentials not set. Add or select API credentials to proceed." };
            }

            if (!String.IsNullOrEmpty(managedsite.RequestConfig.ChallengeProvider))
            {
                providerDefinition = Models.Config.ChallengeProviders.Providers.FirstOrDefault(p => p.Id == managedsite.RequestConfig.ChallengeProvider);
            }
            else
            {
                return new ActionResult { IsSuccess = false, Message = "DNS Challenge API Provider not set. Select an API to proceed." };
            }

            if (providerDefinition.HandlerType == Models.Config.ChallengeHandlerType.PYTHON_HELPER)
            {
                dnsAPIProvider = new LibcloudDNSProvider(credentialArray);
            }
            else
            {
                if (providerDefinition.HandlerType == Models.Config.ChallengeHandlerType.INTERNAL)
                {
                    if (providerDefinition.Id == "DNS01.API.Route53")
                    {
                        zoneId = credentialArray[2];
                        dnsAPIProvider = new Providers.DNS.AWSRoute53.DnsProviderAWSRoute53(credentialArray[0], credentialArray[1]);
                    }
                }
            }

            if (dnsAPIProvider != null)
            {
                var result = await dnsAPIProvider.CreateRecord(new DnsCreateRecordRequest
                {
                    RecordType = "TXT",
                    TargetDomainName = domain,
                    RecordName = txtRecordName,
                    RecordValue = txtRecordValue,
                    ZoneId = zoneId
                });

                if (result.IsSuccess)
                {
                    // do our own txt record query before proceeding with challenge completion
                    /*
                    int attempts = 3;
                    bool recordCheckedOK = false;
                    var networkUtil = new NetworkUtils(false);

                    while (attempts > 0 && !recordCheckedOK)
                    {
                        recordCheckedOK = networkUtil.CheckDNSRecordTXT(domain, txtRecordName, txtRecordValue);
                        attempts--;
                        if (!recordCheckedOK)
                        {
                            await Task.Delay(1000); // hold on a sec
                        }
                    }
                    */
                    return result;
                }
                else
                {
                    return result;
                }
            }
            else
            {
                return new ActionResult { IsSuccess = false, Message = "Error: Could not determine DNS API Provider." };
            }
        }
    }
}