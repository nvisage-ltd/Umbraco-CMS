﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.ViewModels.Template.Query;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Cms.Api.Management.Controllers.Template.Query;

public class SettingsTemplateQueryController : TemplateQueryControllerBase
{
    private readonly IContentTypeService _contentTypeService;

    public SettingsTemplateQueryController(IContentTypeService contentTypeService)
        => _contentTypeService = contentTypeService;

    [HttpGet("settings")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TemplateQuerySettingsViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<TemplateQuerySettingsViewModel>> Settings()
    {
        var contentTypeAliases = _contentTypeService
            .GetAll()
            .Where(contentType => contentType.IsElement == false)
            .Select(contentType => contentType.Alias)
            .ToArray();

        IEnumerable<TemplateQueryPropertyViewModel> properties = GetProperties();

        IEnumerable<TemplateQueryOperatorViewModel> operators = GetOperators();

        return await Task.FromResult(Ok(new TemplateQuerySettingsViewModel
        {
            ContentTypeAliases = contentTypeAliases,
            Properties = properties,
            Operators = operators
        }));
    }
}
