using Microsoft.AspNetCore.Mvc;
using CmsBff.Data.Entities;
using CmsBff.Services;

namespace CmsBff.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CmsController : ControllerBase
{
    private readonly CmsContentService _contentService;
    private readonly CmsTemplateService _templateService;
    private readonly CmsAssetService _assetService;

    public CmsController(
        CmsContentService contentService,
        CmsTemplateService templateService,
        CmsAssetService assetService)
    {
        _contentService = contentService;
        _templateService = templateService;
        _assetService = assetService;
    }

    // Content endpoints
    [HttpGet("content")]
    public async Task<ActionResult<IEnumerable<CmsContent>>> GetAllContent()
    {
        var content = await _contentService.GetAllAsync();
        return Ok(content);
    }

    [HttpGet("content/{id}")]
    public async Task<ActionResult<CmsContent>> GetContent(int id)
    {
        var content = await _contentService.GetByIdAsync(id);
        if (content == null)
            return NotFound();

        return Ok(content);
    }

    [HttpGet("content/slug/{slug}")]
    public async Task<ActionResult<CmsContent>> GetContentBySlug(string slug)
    {
        var content = await _contentService.GetBySlugAsync(slug);
        if (content == null)
            return NotFound();

        return Ok(content);
    }

    [HttpPost("content")]
    public async Task<ActionResult<CmsContent>> CreateContent(CmsContent content)
    {
        var createdContent = await _contentService.CreateAsync(content);
        return CreatedAtAction(nameof(GetContent), new { id = createdContent.Id }, createdContent);
    }

    [HttpPut("content/{id}")]
    public async Task<ActionResult<CmsContent>> UpdateContent(int id, CmsContent content)
    {
        var updatedContent = await _contentService.UpdateAsync(id, content);
        if (updatedContent == null)
            return NotFound();

        return Ok(updatedContent);
    }

    [HttpDelete("content/{id}")]
    public async Task<ActionResult> DeleteContent(int id)
    {
        var deleted = await _contentService.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // Template endpoints
    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<CmsTemplate>>> GetAllTemplates()
    {
        var templates = await _templateService.GetAllAsync();
        return Ok(templates);
    }

    [HttpGet("templates/{id}")]
    public async Task<ActionResult<CmsTemplate>> GetTemplate(int id)
    {
        var template = await _templateService.GetByIdAsync(id);
        if (template == null)
            return NotFound();

        return Ok(template);
    }

    [HttpPost("templates")]
    public async Task<ActionResult<CmsTemplate>> CreateTemplate(CmsTemplate template)
    {
        var createdTemplate = await _templateService.CreateAsync(template);
        return CreatedAtAction(nameof(GetTemplate), new { id = createdTemplate.Id }, createdTemplate);
    }

    [HttpPut("templates/{id}")]
    public async Task<ActionResult<CmsTemplate>> UpdateTemplate(int id, CmsTemplate template)
    {
        var updatedTemplate = await _templateService.UpdateAsync(id, template);
        if (updatedTemplate == null)
            return NotFound();

        return Ok(updatedTemplate);
    }

    [HttpDelete("templates/{id}")]
    public async Task<ActionResult> DeleteTemplate(int id)
    {
        var deleted = await _templateService.DeleteAsync(id);
        if (!deleted)
            return BadRequest("Cannot delete template that is in use");

        return NoContent();
    }

    // Asset endpoints
    [HttpGet("assets")]
    public async Task<ActionResult<IEnumerable<CmsAsset>>> GetAllAssets()
    {
        var assets = await _assetService.GetAllAsync();
        return Ok(assets);
    }

    [HttpGet("assets/{id}")]
    public async Task<ActionResult<CmsAsset>> GetAsset(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null)
            return NotFound();

        return Ok(asset);
    }

    [HttpPost("assets")]
    public async Task<ActionResult<CmsAsset>> CreateAsset(CmsAsset asset)
    {
        var createdAsset = await _assetService.CreateAsync(asset);
        return CreatedAtAction(nameof(GetAsset), new { id = createdAsset.Id }, createdAsset);
    }

    [HttpPut("assets/{id}")]
    public async Task<ActionResult<CmsAsset>> UpdateAsset(int id, CmsAsset asset)
    {
        var updatedAsset = await _assetService.UpdateAsync(id, asset);
        if (updatedAsset == null)
            return NotFound();

        return Ok(updatedAsset);
    }

    [HttpDelete("assets/{id}")]
    public async Task<ActionResult> DeleteAsset(int id)
    {
        var deleted = await _assetService.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}