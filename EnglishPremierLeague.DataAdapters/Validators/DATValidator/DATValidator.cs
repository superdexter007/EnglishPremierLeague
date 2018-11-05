﻿using EnglishPremierLeague.Common.Entities;
using EnglishPremierLeague.Data.Adapters.Validators;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace EnglishPremierLeague.Data.Validators.DATValidator
{
	public class DATValidator: DataValidator
	{
		private readonly ILogger<DATValidator> _logger;
		private int[] _splitLengthArray;

		public List<Column> Columns { get; set; }

		public DATValidator(ILoggerFactory loggerFactory)
		{
			var columnList = new XmlSerializer(typeof(List<Column>), new XmlRootAttribute("Columns"));
			Columns = (List<Column>)columnList.Deserialize(new FileStream(@".\Validators\DATValidator\DATTemplate.xml", FileMode.Open));			
			_splitLengthArray = Columns.Select(t => t.Length).ToArray();
			_logger = loggerFactory.CreateLogger<DATValidator>();
		}

		public override bool Validate(string rowData, bool isHeaderRow, out Team team)
		{
			bool isValid = false;
			team = null;
			
			var columnValues = SplitByLength(rowData, _splitLengthArray);

			if (!ValidateColumnCount(columnValues.Length, Columns.Count))
			{
				_logger.LogDebug("Column count does not match");
				if (isHeaderRow)
					throw new Exception("Column count does not match with the template");

				return isValid;
			}


			if (isHeaderRow)
			{
				_logger.LogDebug("Header row. Validating column names with the template");
				foreach (var columnValue in columnValues)
				{
					if (!ValidateColumnName(columnValue.Trim(), Columns.Find(t => t.Index == (Array.IndexOf(columnValues, columnValue) + 1)).Name))
						return isValid;
				}
				isValid = true;
			}
			else
			{
				_logger.LogDebug("Data row. Validating data with the template for data type");
				team = new Team();
				foreach (var columnValue in columnValues.Select((value, index) => new { index, value }))
				{
					Column column = Columns.Find(t => t.Index == (columnValue.index + 1));
					var columnType = Type.GetType(column.Type);
					object convertedValue;
					if (ValidateColumnType(columnValue.value, columnType, out convertedValue))
					{
						var propertyInfo = (team.GetType()).GetProperty(column.PropertyName);
						if (propertyInfo != null)
							propertyInfo.SetValue(team, convertedValue);
					}
					else
					{
						_logger.LogDebug("Row data is not valid. Ignoring row data");
						_logger.LogDebug(rowData);
						return isValid;
					}

				}
				isValid = true;
			}

			return isValid;
		}
	}
}