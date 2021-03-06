﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Domain.Search;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.PricingModule.Data.Model;

namespace VirtoCommerce.PricingModule.Data.Search
{
    public class ProductPriceDocumentChangesProvider : IIndexDocumentChangesProvider
    {
        public const string ChangeLogObjectType = nameof(PriceEntity);

        private readonly IChangeLogService _changeLogService;
        private readonly IPricingService _pricingService;

        public ProductPriceDocumentChangesProvider(IChangeLogService changeLogService, IPricingService pricingService)
        {
            _changeLogService = changeLogService;
            _pricingService = pricingService;
        }

        public virtual Task<long> GetTotalChangesCountAsync(DateTime? startDate, DateTime? endDate)
        {
            long result;

            if (startDate == null && endDate == null)
            {
                // We don't know the total products count
                result = 0L;
            }
            else
            {
                // Get changes count from operation log
                result = _changeLogService.FindChangeHistory(ChangeLogObjectType, startDate, endDate).Count();
            }

            return Task.FromResult(result);
        }

        public virtual Task<IList<IndexDocumentChange>> GetChangesAsync(DateTime? startDate, DateTime? endDate, long skip, long take)
        {
            IList<IndexDocumentChange> result;

            if (startDate == null && endDate == null)
            {
                result = null;
            }
            else
            {
                // Get changes from operation log
                var operations = _changeLogService.FindChangeHistory(ChangeLogObjectType, startDate, endDate)
                    .Skip((int)skip)
                    .Take((int)take)
                    .ToArray();

                var priceIds = operations.Select(c => c.ObjectId).ToArray();
                var priceIdsAndProductIds = GetProductIds(priceIds);

                result = operations
                    .Where(o => priceIdsAndProductIds.ContainsKey(o.ObjectId))
                    .Select(o => new IndexDocumentChange
                    {
                        DocumentId = priceIdsAndProductIds[o.ObjectId],
                        ChangeDate = o.ModifiedDate ?? o.CreatedDate,
                        ChangeType = IndexDocumentChangeType.Modified,
                    })
                    .ToArray();
            }

            return Task.FromResult(result);
        }


        protected virtual IDictionary<string, string> GetProductIds(string[] priceIds)
        {
            // TODO: How to get product for deleted price?
            var prices = _pricingService.GetPricesById(priceIds);
            var result = prices.ToDictionary(p => p.Id, p => p.ProductId);
            return result;
        }
    }
}
