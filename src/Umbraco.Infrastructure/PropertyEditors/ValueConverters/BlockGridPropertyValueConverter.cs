// Copyright (c) Umbraco.
// See LICENSE for more details.

using Umbraco.Cms.Core.ContentApi;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.ContentApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ContentApi;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.PropertyEditors.BlockGridConfiguration;

namespace Umbraco.Cms.Core.PropertyEditors.ValueConverters
{
    [DefaultPropertyValueConverter(typeof(JsonValueConverter))]
    public class BlockGridPropertyValueConverter : BlockPropertyValueConverterBase<BlockGridModel, BlockGridItem, BlockGridLayoutItem, BlockGridBlockConfiguration>, IContentApiPropertyValueConverter
    {
        private readonly IProfilingLogger _proflog;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApiElementBuilder _apiElementBuilder;

        // Niels, Change: I would love if this could be general, so we don't need a specific one for each block property editor....
        public BlockGridPropertyValueConverter(
            IProfilingLogger proflog, BlockEditorConverter blockConverter,
            IJsonSerializer jsonSerializer,
            IApiElementBuilder apiElementBuilder)
            : base(blockConverter)
        {
            _proflog = proflog;
            _jsonSerializer = jsonSerializer;
            _apiElementBuilder = apiElementBuilder;
        }

        /// <inheritdoc />
        public override bool IsConverter(IPublishedPropertyType propertyType)
            => propertyType.EditorAlias.InvariantEquals(Constants.PropertyEditors.Aliases.BlockGrid);

        public override object? ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
            => ConvertIntermediateToBlockGridModel(propertyType, referenceCacheLevel, inter, preview);

        public Type GetContentApiPropertyValueType(IPublishedPropertyType propertyType)
            => typeof(ApiBlockGridModel);

        public object? ConvertIntermediateToContentApiObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
        {
            const int defaultColumns = 12;

            BlockGridModel? blockGridModel = ConvertIntermediateToBlockGridModel(propertyType, referenceCacheLevel, inter, preview);
            if (blockGridModel == null)
            {
                return new ApiBlockGridModel(defaultColumns, Array.Empty<ApiBlockGridItem>());
            }

            ApiBlockGridItem CreateApiBlockGridItem(BlockGridItem item)
                => new ApiBlockGridItem(
                    _apiElementBuilder.Build(item.Content),
                    item.Settings != null
                        ? _apiElementBuilder.Build(item.Settings)
                        : null,
                    item.RowSpan,
                    item.ColumnSpan,
                    item.AreaGridColumns ?? blockGridModel.GridColumns ?? defaultColumns,
                    item.Areas.Select(CreateApiBlockGridArea).ToArray());

            ApiBlockGridArea CreateApiBlockGridArea(BlockGridArea area)
                => new ApiBlockGridArea(
                    area.Alias,
                    area.RowSpan,
                    area.ColumnSpan,
                    area.Select(CreateApiBlockGridItem).ToArray());

            var model = new ApiBlockGridModel(
                blockGridModel.GridColumns ?? defaultColumns,
                blockGridModel.Select(CreateApiBlockGridItem).ToArray());

            return model;
        }

        private BlockGridModel? ConvertIntermediateToBlockGridModel(IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
        {
            using (_proflog.DebugDuration<BlockGridPropertyValueConverter>($"ConvertPropertyToBlockGrid ({propertyType.DataType.Id})"))
            {
                // Get configuration
                var configuration = propertyType.DataType.ConfigurationAs<BlockGridConfiguration>();
                if (configuration is null)
                {
                    return null;
                }

                BlockGridModel CreateEmptyModel() => BlockGridModel.Empty;

                BlockGridModel CreateModel(IList<BlockGridItem> items) => new BlockGridModel(items, configuration.GridColumns);

                BlockGridItem? EnrichBlockItem(BlockGridItem blockItem, BlockGridLayoutItem layoutItem, BlockGridBlockConfiguration blockConfig, CreateBlockItemModelFromLayout createBlockItem)
                {
                    // enrich block item with additional configs + setup areas
                    var blockConfigAreaMap = blockConfig.Areas.ToDictionary(area => area.Key);

                    blockItem.RowSpan = layoutItem.RowSpan!.Value;
                    blockItem.ColumnSpan = layoutItem.ColumnSpan!.Value;
                    blockItem.AreaGridColumns = blockConfig.AreaGridColumns;
                    blockItem.GridColumns = configuration.GridColumns;
                    blockItem.Areas = layoutItem.Areas.Select(area =>
                    {
                        if (!blockConfigAreaMap.TryGetValue(area.Key, out BlockGridAreaConfiguration? areaConfig))
                        {
                            return null;
                        }

                        var items = area.Items.Select(item => createBlockItem(item)).WhereNotNull().ToList();
                        return new BlockGridArea(items, areaConfig.Alias!, areaConfig.RowSpan!.Value, areaConfig.ColumnSpan!.Value);
                    }).WhereNotNull().ToArray();

                    return blockItem;
                }

                BlockGridModel blockModel = UnwrapBlockModel(
                    referenceCacheLevel,
                    inter,
                    preview,
                    configuration.Blocks,
                    CreateEmptyModel,
                    CreateModel,
                    EnrichBlockItem
                );

                return blockModel;
            }
        }

        protected override BlockEditorDataConverter CreateBlockEditorDataConverter() => new BlockGridEditorDataConverter(_jsonSerializer);

        protected override BlockItemActivator<BlockGridItem> CreateBlockItemActivator() => new BlockGridItemActivator(BlockEditorConverter);

        private class BlockGridItemActivator : BlockItemActivator<BlockGridItem>
        {
            public BlockGridItemActivator(BlockEditorConverter blockConverter) : base(blockConverter)
            {
            }

            protected override Type GenericItemType => typeof(BlockGridItem<,>);
        }
    }
}
