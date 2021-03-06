﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using Nest.TestData.Domain;

namespace Nest.Tests.Dsl.Json.Filter
{
	[TestFixture]
	public class SpanTermQueryJson
	{
		[Test]
		public void SpanTermQuery()
		{
			var s = new SearchDescriptor<ElasticSearchProject>()
				.From(0)
				.Size(10)
				.Query(q => q
					.SpanTerm(f=>f.Name, "elasticsearch.pm", 1.1)
				);
			var json = ElasticClient.Serialize(s);
			var expected = @"{ from: 0, size: 10, query : 
			{ span_term: { 
				name: {
					value: ""elasticsearch.pm"",
					boost: 1.1
				}
			}}}";
			Assert.True(json.JsonEquals(expected), json);
		}
	}
}
