﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Newtonsoft.Json.Converters;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Nest
{
	public partial class ElasticClient
	{
		private readonly string _aliasBody = @"{{""actions"" : [{0}] }}";
		private string _createCommand(string command, AliasParams aliasParam)
		{
			var cmd  = @"{{ ""{0}"" : {{
				index: ""{1}"",
				alias: ""{2}""".F(command, aliasParam.Index, aliasParam.Alias);

			if (!aliasParam.Filter.IsNullOrEmpty())
				cmd += @", ""filter"": {0} ".F(aliasParam.Filter);

			if (!aliasParam.Routing.IsNullOrEmpty())
				cmd += @", ""routing"": ""{0}"" ".F(aliasParam.Routing);
			else
			{
				if (!aliasParam.IndexRouting.IsNullOrEmpty())
					cmd += @", ""index_routing"": ""{0}"" ".F(aliasParam.IndexRouting);
				if (!aliasParam.SearchRouting.IsNullOrEmpty())
					cmd += @", ""search_routing"": ""{0}"" ".F(aliasParam.SearchRouting);
			}
			cmd += "} }";

			return cmd;
		}

	/// <summary>
	/// Get all the indices pointing to an alias
	/// </summary>
	public IEnumerable<string> GetIndicesPointingToAlias(string alias)
	{
	  var path = this.CreatePath(alias) + "/_aliases";
	  var status = this.Connection.GetSync(path);
	  var r = JsonConvert.DeserializeObject<Dictionary<string, object>>(status.Result, DeserializeSettings);
	  return r == null ? Enumerable.Empty<string>() : r.Keys;
	}

	/// <summary>
	/// Rename an old alias for index to a new alias in one operation
	/// </summary>
	public IndicesOperationResponse Swap(string alias, IEnumerable<string> oldIndices, IEnumerable<string> newIndices)
	{
	  var commands = new List<string>();
	  foreach (var i in oldIndices)
		commands.Add(_createCommand("remove", new AliasParams { Index = i, Alias = alias }));
	  foreach (var i in newIndices)
		commands.Add(_createCommand("add", new AliasParams { Index = i, Alias = alias }));
	  return this._Alias(string.Join(", ", commands));
	}

		/// <summary>
		/// Add an alias to the default index
		/// </summary>
		public IndicesOperationResponse Alias(string alias)
		{
			var index = this.Settings.DefaultIndex;
			var q = _createCommand("add", new AliasParams { Index = index, Alias = alias });
			return this._Alias(q);
		}
		/// <summary>
		/// Add an alias to the specified index
		/// </summary>
		public IndicesOperationResponse Alias(string index, string alias)
		{
			var q = _createCommand("add", new AliasParams { Index = index, Alias = alias });
			return this._Alias(q);
		}
		/// <summary>
		/// Add multiple aliases to the specified index
		/// </summary>
		public IndicesOperationResponse Alias(string index, IEnumerable<string> aliases)
		{
			aliases.Select(a=> _createCommand("add", new AliasParams { Index = index, Alias = a }));
			var q = string.Join(",", aliases);
			return this._Alias(q);
		}
		/// <summary>
		/// Add multiple aliases to the default index
		/// </summary>
		public IndicesOperationResponse Alias(IEnumerable<string> aliases)
		{
			var index = this.Settings.DefaultIndex;
			aliases.Select(a=> _createCommand("add", new AliasParams { Index = index, Alias = a }));
			var q = string.Join(",", aliases);
			return this._Alias(q);
		}
		/// <summary>
		/// Remove an alias for the default index
		/// </summary>
		public IndicesOperationResponse RemoveAlias(string alias)
		{
			var index = this.Settings.DefaultIndex;
			var q = _createCommand("remove", new AliasParams { Index = index, Alias = alias });
			return this._Alias(q);
		}
		/// <summary>
		/// Remove an alias for the specified index
		/// </summary>
		public IndicesOperationResponse RemoveAlias(string index, string alias)
		{
			var q = _createCommand("remove", new AliasParams { Index = index, Alias = alias });
			return this._Alias(q);
		}
		/// <summary>
		/// Remove multiple alias for the default index
		/// </summary>
		public IndicesOperationResponse RemoveAlias(IEnumerable<string> aliases)
		{
			var index = this.Settings.DefaultIndex;
			aliases.Select(a => _createCommand("remove", new AliasParams { Index = index, Alias = a }));
			var q = string.Join(",", aliases);
			return this._Alias(q);
		}
		/// <summary>
		/// Remove multiple alias for the specified index
		/// </summary>
		public IndicesOperationResponse RemoveAlias(string index, IEnumerable<string> aliases)
		{
			aliases.Select(a => _createCommand("remove", new AliasParams { Index = index, Alias = a }));
			var q = string.Join(",", aliases);
			return this._Alias(q);
		}
		/// <summary>
		/// Associate multiple indices with one alias
		/// </summary>
		public IndicesOperationResponse Alias(IEnumerable<string> indices, string alias)
		{
			indices.Select(i => _createCommand("add", new AliasParams { Index = i, Alias = alias }));
			var q = string.Join(",", indices);
			return this._Alias(q);
		}
		/// <summary>
		/// Rename an old alias for index to a new alias in one operation
		/// </summary>
		public IndicesOperationResponse Rename(string alias, string oldIndex, string newIndex)
		{
			var r = _createCommand("remove", new AliasParams { Index = oldIndex, Alias = alias });
			var a = _createCommand("add", new AliasParams { Index = newIndex, Alias = alias });
			return this._Alias(r + ", " + a);
		}
		/// <summary>
		/// Freeform alias overload for complete control of all the aspects (does an add operation)
		/// </summary>
		public IndicesOperationResponse Alias(AliasParams aliasParams)
		{
			return this._Alias(_createCommand("add", aliasParams));
		}
		/// <summary>
		/// Freeform multi alias overload for complete control of all the aspects (does multiple add operations)
		/// </summary>
		public IndicesOperationResponse Alias(IEnumerable<AliasParams> aliases)
		{
			var cmds = aliases.Select(a=>_aliasBody.F(_createCommand("add", a)));
			var q = string.Join(",", aliases);
			return this._Alias(q);
		}
		/// <summary>
		/// Freeform remove alias overload for complete control of all the aspects
		/// </summary>
		public IndicesOperationResponse RemoveAlias(AliasParams aliasParams)
		{
			return this._Alias(_createCommand("remove", aliasParams));
		}
		/// <summary>
		/// Freeform remove multi alias overload for complete control of all the aspects
		/// </summary>
		public IndicesOperationResponse RemoveAliases(IEnumerable<AliasParams> aliases)
		{
			var cmds = aliases.Select(a => _aliasBody.F(_createCommand("remove", a)));
			var q = string.Join(",", aliases);
			return this._Alias(q);
		}
		private IndicesOperationResponse _Alias(string query)
		{
			var path = "/_aliases";
			query = _aliasBody.F(query);
			var status = this.Connection.PostSync(path, query);			
			
			var r = this.ToParsedResponse<IndicesOperationResponse>(status);
			return r;
		}
	}
}
