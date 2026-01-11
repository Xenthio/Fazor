using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using System.Collections.Generic;

namespace Sandbox.Razor;

/// <summary>
/// Provides bind tag helpers for common elements and components.
/// This enables @bind-value syntax without requiring runtime component discovery.
/// </summary>
internal class BindTagHelperProvider : ITagHelperDescriptorProvider
{
	public int Order => 1000;

	public RazorEngine Engine { get; set; } = null!;

	public void Execute(TagHelperDescriptorProviderContext context)
	{
		if (context == null)
		{
			throw new System.ArgumentNullException(nameof(context));
		}

		// Add fallback bind tag helper for all elements
		// This allows @bind-value="..." to work on any element
		var fallbackBind = CreateBindTagHelper(
			tagName: "*",
			valueAttributeName: "value",
			changeAttributeName: "onchange",
			isFallback: true
		);
		context.Results.Add(fallbackBind);

		// Add specific bind tag helpers for common controls
		// TextEntry: @bind-Value="..." maps to Value and ValueChanged
		var textEntryBind = CreateBindTagHelper(
			tagName: "textentry",
			valueAttributeName: "Value",
			changeAttributeName: "ValueChanged",
			isFallback: false
		);
		context.Results.Add(textEntryBind);

		// Input element: @bind-value="..." maps to value and onchange
		var inputBind = CreateBindTagHelper(
			tagName: "input",
			valueAttributeName: "value",
			changeAttributeName: "onchange",
			isFallback: false
		);
		context.Results.Add(inputBind);
	}

	private TagHelperDescriptor CreateBindTagHelper(
		string tagName,
		string valueAttributeName,
		string changeAttributeName,
		bool isFallback)
	{
		var builder = TagHelperDescriptorBuilder.Create(
			ComponentMetadata.Bind.TagHelperKind,
			"Bind",
			typeof(BindTagHelperProvider).Assembly.GetName().Name ?? "Sandbox.Razor"
		);

		builder.SetTypeName("Microsoft.AspNetCore.Components.Bind");

		// Add tag matching rule with required attribute
		builder.TagMatchingRule(rule =>
		{
			rule.TagName = tagName;
			
			// Require the presence of either @bind or @bind-* attribute
			rule.Attribute(attr =>
			{
				attr.Name = "bind-";
				attr.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
				attr.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
			});
		});

		// Add the bind directive attribute
		builder.BindAttribute(attribute =>
		{
			attribute.Name = "bind";
			attribute.TypeName = "System.Object";
			
			// Mark as directive attribute
			attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;

			// Store metadata about the bind
			attribute.Metadata[ComponentMetadata.Bind.ValueAttribute] = valueAttributeName;
			attribute.Metadata[ComponentMetadata.Bind.ChangeAttribute] = changeAttributeName;
		});

		// Add bind-{property} directive attribute pattern
		builder.BindAttribute(attribute =>
		{
			attribute.Name = "bind-";
			attribute.IndexerAttributeNamePrefix = "bind-";
			attribute.TypeName = "System.Object";
			
			// Mark as directive attribute
			attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
			
			// The value attribute name will be computed from the bind-{property} syntax
			// e.g., bind-Value means the value attribute is "Value"
		});

		// Mark as bind tag helper
		builder.Metadata[ComponentMetadata.SpecialKindKey] = ComponentMetadata.Bind.TagHelperKind;

		// Mark value and change attributes
		builder.Metadata[ComponentMetadata.Bind.ValueAttribute] = valueAttributeName;
		builder.Metadata[ComponentMetadata.Bind.ChangeAttribute] = changeAttributeName;

		// Mark as fallback if needed
		if (isFallback)
		{
			builder.Metadata[ComponentMetadata.Bind.FallbackKey] = bool.TrueString;
		}

		return builder.Build();
	}
}
