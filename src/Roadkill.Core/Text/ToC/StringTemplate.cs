﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadkill.Core.Text.ToC
{
	internal class StringTemplate
	{
		private string _htmlOutput;

		public string ItemStart { get; set; }
		public string ItemEnd { get; set; }
		public string LevelStart { get; set; }
		public string LevelEnd { get; set; }
		public string ItemFormat { get; set; }

		public string ReplaceTokensWithValues(Item item)
		{
			///<li><a href=""#{id}"">{level}.{itemnumber}&nbsp;{title}</a></li>
			string result = ItemFormat;
			result = result.Replace("{id}", item.Id);
			result = result.Replace("{levels}", GetLevelText(item));

			if (item.Level > 1)
			{
				result = result.Replace("{itemnumber}", item.GetPositionAmongSiblings().ToString());
			}
			else
			{
				result = result.Replace("{itemnumber}", item.GetPositionAmongSiblings().ToString() + ".");
			}

			result = result.Replace("{title}", item.Title);

			return result;
		}

		private string GetLevelText(Item item)
		{
			// Anything at level 1 should use the counter for its number, for example
			// 1. H1, 2. H2 (and Level 0 is just a holder level, not used except to balance the tree)
			if (item.Level > 1)
			{
				// Traverse back to the root, getting the index position of each parent amongst its siblings
				List<int> positions = new List<int>();
				Item itemParent = item.Parent;

				positions.Add(itemParent.GetPositionAmongSiblings());
				while (itemParent != null && itemParent.Parent != null && itemParent.Level > 1)
				{
					itemParent = itemParent.Parent;
					positions.Add(itemParent.GetPositionAmongSiblings());
				}

				positions.Reverse();
				return string.Join(".", positions.ToArray()) + ".";
			}
			else
			{
				// H1s are the root items, so use their position
				return "";
			}
		}

		public string CreateHtml(Item parent)
		{
			_htmlOutput = "";
			GenerateHtmlFromItem(parent);
			return _htmlOutput;
		}

		private void GenerateHtmlFromItem(Item parent)
		{
			foreach (Item item in parent.Children)
			{
				_htmlOutput += ItemStart;
				_htmlOutput += ReplaceTokensWithValues(item);

				if (item.HasChildren())
				{
					_htmlOutput += LevelStart;
					GenerateHtmlFromItem(item);
					_htmlOutput += LevelEnd;
				}

				_htmlOutput += ItemEnd;
			}
		}
	}
}