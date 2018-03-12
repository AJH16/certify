﻿using Amazon.Route53;
using Amazon.Route53.Model;
using Certify.Models.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certify.Providers.DNS.AWSRoute53
{
    public class DnsProviderAWSRoute53 : IDnsProvider
    {
        private AmazonRoute53Client route53Client;

        public DnsProviderAWSRoute53(string accessKey, string secretKey)
        {
            route53Client = new AmazonRoute53Client(accessKey, secretKey);
        }

        private async Task<HostedZone> ResolveMatchingZone(DnsRecordRequest request)
        {
            try
            {
                if (!String.IsNullOrEmpty(request.ZoneId))
                {
                    var zone = await route53Client.GetHostedZoneAsync(new GetHostedZoneRequest { Id = request.ZoneId });
                    return zone.HostedZone;
                }
                else
                {
                    var zones = route53Client.ListHostedZones();
                    var zone = zones.HostedZones.Where(z => z.Name.Contains(request.TargetDomainName)).FirstOrDefault();
                    return zone;
                }
            }
            catch (Exception exp)
            {
                //TODO: return error in result
                return null;
            }
        }

        private async Task<bool> ApplyDnsChange(HostedZone zone, ResourceRecordSet recordSet, ChangeAction action)
        {
            // prepare change
            var changeDetails = new Change()
            {
                ResourceRecordSet = recordSet,
                Action = action
            };

            var changeBatch = new ChangeBatch()
            {
                Changes = new List<Change> { changeDetails }
            };

            // Update the zone's resource record sets
            var recordsetRequest = new ChangeResourceRecordSetsRequest()
            {
                HostedZoneId = zone.Id,
                ChangeBatch = changeBatch
            };

            var recordsetResponse = route53Client.ChangeResourceRecordSets(recordsetRequest);

            // Monitor the change status
            var changeRequest = new GetChangeRequest()
            {
                Id = recordsetResponse.ChangeInfo.Id
            };

            while (ChangeStatus.PENDING == route53Client.GetChange(changeRequest).ChangeInfo.Status)
            {
                System.Diagnostics.Debug.WriteLine("DNS change is pending.");
                await Task.Delay(2500);
            }
            return true;
        }

        public async Task<DnsRequestResult> CreateRecord(DnsCreateRecordRequest request)
        {
            // https://docs.aws.amazon.com/sdk-for-net/v2/developer-guide/route53-apis-intro.html
            // find zone
            var zone = await ResolveMatchingZone(request);

            if (zone != null)
            {
                var recordSet = new ResourceRecordSet()
                {
                    Name = request.RecordName,
                    TTL = 5,
                    Type = RRType.TXT,
                    ResourceRecords = new List<ResourceRecord>
                        {
                          new ResourceRecord { Value = request.RecordValue }
                        }
                };

                var result = await ApplyDnsChange(zone, recordSet, ChangeAction.UPSERT);

                return new DnsRequestResult { IsSuccess = true, Message = "Success" };
            }
            else
            {
                return new DnsRequestResult { IsSuccess = false, Message = "DNS Zone match could not be determined." };
            }
        }

        public async Task<DnsRequestResult> DeleteRecord(DnsDeleteRecordRequest request)
        {
            var zone = await ResolveMatchingZone(request);

            if (zone != null)
            {
                var recordSet = new ResourceRecordSet()
                {
                    Name = request.RecordName,
                    TTL = 5,
                    Type = RRType.TXT,
                    ResourceRecords = new List<ResourceRecord>
                        {
                          new ResourceRecord { Value = request.RecordValue }
                        }
                };

                var result = ApplyDnsChange(zone, recordSet, ChangeAction.DELETE);

                return new DnsRequestResult { IsSuccess = true, Message = "Success" };
            }
            else
            {
                return new DnsRequestResult { IsSuccess = false, Message = "DNS Zone match could not be determined." };
            }
        }
    }
}