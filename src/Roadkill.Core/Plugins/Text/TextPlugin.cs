﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Roadkill.Core.Configuration;
using Roadkill.Core.Database;
using Roadkill.Core.Logging;
using StructureMap;

namespace Roadkill.Core.Plugins
{
	/// <summary>
	/// Work in progress
	/// </summary>
	public abstract class TextPlugin
	{
		public static readonly string PARSER_IGNORE_STARTTOKEN = "{{{roadkillinternal";
		public static readonly string PARSER_IGNORE_ENDTOKEN = "roadkillinternal}}}";

		private List<string> _scriptFiles;
		private string _onLoadFunction;
		private Guid _databaseId;
		private Settings _settings;
		private string _pluginVirtualPath;

		public Settings Settings
		{
			get
			{
				return _settings;
			}
			set
			{
				if (value != null)
					_settings = value;
			}
		}

		/// <summary>
		/// The unique ID for the plugin, which is also the directory it's stored in inside the /Plugins/ directory.
		/// This should not be case sensitive.
		/// </summary>
		public abstract string Id { get; }
		public abstract string Name { get; }
		public abstract string Description { get; }
		public abstract string Version { get; }

		public ApplicationSettings ApplicationSettings { get; set; }
		public SiteSettings SiteSettings { get; set; }
		public virtual bool IsCacheable { get; set; }

		/// <summary>
		/// The virtual path for the plugin, e.g. ~/Plugins/Text/MyPlugin/. Does not contain a trailing slash.
		/// </summary>
		public string PluginVirtualPath
		{
			get
			{
				if (_pluginVirtualPath == null)
				{
					EnsureIdIsValid();
					_pluginVirtualPath = "~/Plugins/Text/" + Id;
				}

				return _pluginVirtualPath;
			}
		}

		/// <summary>
		/// Used as the PK in the site_configuration table to store the plugin settings.
		/// </summary>
		public Guid DatabaseId
		{
			get
			{
				// Generate an ID for use in the database in the format:
				// {aaaaaaaa-0000-0000-0000-000000000000}
				// Where a = hashcode of the plugin id
				// 
				// It's not globally unique, but it doesn't matter as it's 
				// being used for the site_configuration db table only. The only 
				// way the Guid could clash is if two plugins have the same ID.
				// This should never happen, as the IDs will be like nuget ids.
				//
				if (_databaseId == Guid.Empty)
				{
					EnsureIdIsValid();
					int firstPart = Id.GetHashCode();

					short zero = (short)0;
					byte[] lastChunk = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

					_databaseId = new Guid(firstPart, zero, zero, lastChunk);
				}

				return _databaseId;
			}
		}

		public TextPlugin(ApplicationSettings applicationSettings, IRepository repository)
		{
			_scriptFiles = new List<string>();

			ApplicationSettings = applicationSettings;
			IsCacheable = true;
			Settings = new Settings();

			if (repository != null)
				SiteSettings = repository.GetSiteSettings();
		}

		public virtual string BeforeParse(string markupText)
		{
			return markupText;
		}

		public virtual string AfterParse(string html)
		{
			return html;
		}

		public string GetSettingsJson()
		{
			return JsonConvert.SerializeObject(this.Settings, Formatting.Indented);
		}

		public virtual string RemoveParserIgnoreTokens(string html)
		{
			html = html.Replace(PARSER_IGNORE_STARTTOKEN, "");
			html = html.Replace(PARSER_IGNORE_ENDTOKEN, "");

			return html;
		}

		public virtual string GetHeadContent()
		{
			return "";
		}

		public virtual string GetFooterContent()
		{
			return "";
		}

		/// <summary>
		/// Gets the HTML for a javascript link for the plugin, assuming the javascript is stored in the /Plugins/ID/ folder.
		/// </summary>
		public string GetJavascriptHtml()
		{
			string headScript = "<script type=\"text/javascript\">";
			headScript += "head.js(";
			headScript += string.Join(",\n", _scriptFiles);
			headScript += ",function() { " +_onLoadFunction+ " })";
			headScript += "</script>\n";

			return headScript;
		}

		public void SetHeadJsOnLoadedFunction(string functionBody)
		{
			_onLoadFunction = functionBody;
		}

		public void AddScript(string filename, string name = "", bool useHeadJs = true)
		{
			if (useHeadJs)
			{
				string fileLink = "{ \"[name]\", \"[filename]\" }";
				if (string.IsNullOrEmpty(name))
				{
					fileLink = "\"[filename]\"";
				}

				// Get the server path
				if (HttpContext.Current != null)
				{
					UrlHelper urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
					filename = string.Concat(urlHelper.Content(PluginVirtualPath), "/", filename);
				}

				fileLink = fileLink.Replace("[name]", name);
				fileLink = fileLink.Replace("[filename]", filename);

				_scriptFiles.Add(fileLink);
			}
			else
			{
				Log.Error("Only Head JS is currently supported for plugin Javascript links");
			}
		}

		/// <summary>
		/// Gets the HTML for a CSS link for the plugin, assuming the CSS is stored in the /Plugins/ID/ folder.
		/// </summary>
		public string GetCssLink(string filename)
		{
			string cssLink = "\t\t<link href=\"{0}/{1}\" rel=\"stylesheet\" type=\"text/css\" />\n";
			string html = "";

			if (HttpContext.Current != null)
			{
				UrlHelper urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
				html = string.Format(cssLink, urlHelper.Content(PluginVirtualPath), filename);
			}
			else
			{
				html = string.Format(cssLink, PluginVirtualPath, filename);
			}

			return html;
		}

		/// <summary>
		/// Adds a token to the start and end of the the provided token, so it'll be ignored 
		/// by the parser. This is necessary for tokens such as [[ and {{ which the parser will 
		/// try to parse.
		/// </summary>
		/// <param name="token">The token the plugin uses. This can be a regex.</param>
		public static string ParserSafeToken(string token)
		{
			// The new lines are important for the current Creole parser to recognise the ignore token.
			return "\n" + PARSER_IGNORE_STARTTOKEN + " \n" + token + "\n" + PARSER_IGNORE_ENDTOKEN + "\n";
		}

		private void EnsureIdIsValid()
		{
			if (string.IsNullOrEmpty(Id))
				throw new PluginException(null, "The ID is empty or null for plugin {0}. Please remove this plugin from the bin and plugins folder.", this.GetType().Name);
		}
	}
}