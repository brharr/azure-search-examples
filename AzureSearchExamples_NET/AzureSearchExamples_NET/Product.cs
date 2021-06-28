using System;
using System.Text.Json.Serialization;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace AzureSearchExamples_NET
{
	public partial class Product
	{
		[SimpleField(IsKey = true, IsFilterable = true)]
		public string ProductID { get; set; }

		[SearchableField(IsSortable = true)]
		public string Name { get; set; }

		[SearchableField(IsFilterable = true)]
		public string ProductNumber { get; set; }

		[SearchableField(IsSortable = true)]
		public string ListPrice { get; set; }

		[SearchableField(IsSortable = true, IsFilterable = true)]
		public string Size { get; set; }

		[SearchableField(IsSortable = true, IsFilterable = true)]
		public string Color { get; set; }

		[SimpleField(IsFilterable = true)]
		public string DiscontinuedDate { get; set; }

		[SimpleField(IsFacetable = true, IsHidden = true)]
		public string ProductCategoryID { get; set; }
	}
}

