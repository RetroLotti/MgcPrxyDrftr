using System;
using System.Linq;
using System.Collections.Generic;

namespace MgcPrxyDrftr.lib
{
    public static class EnumerableExtensions
    {
        public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
        {
            var enumerable = sequence.ToList();
            var totalWeight = enumerable.Sum(weightSelector);
            // The weight we are after...
            var itemWeightIndex = (float)new Random().NextDouble() * totalWeight;
            float currentWeightIndex = 0;

            foreach (var item in from weightedItem in enumerable select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;

                // If we've hit or passed the weight we are after for this item then it's the one we want....
                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;

            }

            return default;
        }
    }
}