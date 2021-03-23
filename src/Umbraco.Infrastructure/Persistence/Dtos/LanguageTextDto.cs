﻿using System;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Cms.Infrastructure.Persistence.Dtos
{
    [TableName(Cms.Core.Constants.DatabaseSchema.Tables.DictionaryValue)]
    [PrimaryKey("pk")]
    [ExplicitColumns]
    internal class LanguageTextDto
    {
        [Column("pk")]
        [PrimaryKeyColumn]
        public int PrimaryKey { get; set; }

        [Column("languageId")]
        [ForeignKey(typeof(LanguageDto), Column = "id")]
        public int LanguageId { get; set; }

        [Column("UniqueId")]
        [ForeignKey(typeof(DictionaryDto), Column = "id")]
        public Guid UniqueId { get; set; }

        [Column("value")]
        [Length(1000)]
        public string Value { get; set; }
    }
}