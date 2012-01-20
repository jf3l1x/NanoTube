namespace NanoTube
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Core;
	using Support;

	public static class MetricExtensions
	{
		/// <summary>	Lazy converts a list of metrics to strings based on the given MetricFormat. </summary>
		/// <remarks>	1/19/2012. </remarks>
		/// <param name="metrics">	The metrics to act on. </param>
		/// <param name="format"> 	Describes the format to use. </param>
		/// <returns>	An enumerator that allows foreach to be used to process the strings in this collection. </returns>
		public static IEnumerable<string> ToStrings(this IEnumerable<IMetric> metrics, MetricFormat format)
		{
			foreach (var metric in metrics)
			{
				yield return metric.ToString(format);
			}
		}

		/// <summary>	An IMetric extension method that converts a given metric value into a string representation based on the given MetricFormat. </summary>
		/// <remarks>	1/19/2012. </remarks>
		/// <param name="metric">	The metric to act on. </param>
		/// <param name="format">	Describes the format to use. </param>
		/// <returns>	A string representation of this object. </returns>
		public static string ToString(this IMetric metric, MetricFormat format)
		{
			switch (format)
			{
				case MetricFormat.StatSite:
					return metric.ToStatSiteString();

				case MetricFormat.StatsD:
				default:
					return metric.ToStatsDString();
			}
		}

		//https://github.com/etsy/statsd
		/*
		 * Key / value pair - gorets 12345 timestamp
		 * it doesn't appear statsd has native support for kv pairs and its not documented
		 * Etsys parsing code lives here found other code to suggest it works like this - it appears from the code that a key 
		 * https://raw.github.com/etsy/statsd/master/stats.js
		 * 
		 * Timing - glork:320|ms
		 * The glork took 320ms to complete this time. StatsD figures out 90th percentile, average (mean), lower and upper bounds for the flush interval. The percentile threshold can be tweaked with config.percentThreshold.
		 * 
		 * Counting - gorets:1|c
		 * This is a simple counter. Add 1 to the "gorets" bucket. It stays in memory until the flush interval config.flushInterval.
		 * 
		 * Sampling - gorets:1|c|@0.1
		 * Tells StatsD that this counter is being sent sampled every 1/10th of the time.
		 */
		private static string ToStatsDString(this IMetric metric)
		{
			Type t = metric.GetType();
			if (t == typeof(KeyValue))
			{
				//TODO: 1-19-2012 - this may be entirely wrong - we need to find a statsd to test against
				KeyValue keyValue = (KeyValue)metric;
				return string.Format("{0}:{1:0.###}|ms", keyValue.Key, keyValue.Value, keyValue.TimeStamp.HasValue ? keyValue.TimeStamp.Value.AsUnixTime().ToString() : string.Empty);
			}
			else if (t == typeof(Timing))
			{
				Timing timing = (Timing)metric;
				return String.Format("{0}:{1:0.###}|ms", timing.Key, timing.Elapsed);
			}
			else if (t == typeof(Counter))
			{
				Counter counter = (Counter)metric;
				return String.Format("{0}:{1}|c", counter.Key, counter.Adjustment);
			}
			
			Sample sample = (Sample)metric; //last option
			return String.Format("{0}:{1:0.###}|c|@{2:f}", sample.Key, sample.Value, sample.Frequency);
		}

		//https://github.com/kiip/statsite
		//key:value|type[|@flag]
		/*
		 * Key / Value pair - mysql.queries:1381|kv|@1313107325
		 * Reporting how many queries we've seen in the last second on MySQL:
		 * 
		 * Counting - rewards:1|c
		 * Increments the "rewards" counter by 1 (use -1 to decrement by 1)
		 * 
		 * Timing - api.session_created:114|ms
		 * timing the response speed of an API call:
		 * 
		 * Sampling - api.session_created:114|ms|@0.1
		 * Saying we sample this data in 1/10th of the API requests.
		 */
		private static string ToStatSiteString(this IMetric metric)
		{
			Type t = metric.GetType();
			if (t == typeof(KeyValue))
			{
				KeyValue keyValue = (KeyValue)metric;
				if (keyValue.TimeStamp.HasValue)
				{
					return string.Format("{0}:{1:0.###}|kv|@{2}", keyValue.Key, keyValue.Value, keyValue.TimeStamp.Value.AsUnixTime());
				}
				return string.Format("{0}:{1:0.###}|kv", keyValue.Key, keyValue.Value);
			}
			else if (t == typeof(Counter))
			{
				Counter counter = (Counter)metric;
				return String.Format("{0}:{1}|c", counter.Key, counter.Adjustment);
			}
			else if (t == typeof(Timing))
			{
				Timing timing = (Timing)metric;
				return String.Format("{0}:{1:0.###}|ms", timing.Key, timing.Elapsed);
			}

			Sample sample = (Sample)metric; //last option
			return String.Format("{0}:{1:0.###}|c|@{2:f}", sample.Key, sample.Value, sample.Frequency);
		}
	}
}