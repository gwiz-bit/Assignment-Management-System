using HighSchoolManagementSystem.Services.DTOs;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace HighSchoolManagementSystem.MVC.Models;

// Localize binding failures (such as an unreadable date) as well as annotation errors.
public sealed class ProfileValidationMetadataProvider : IBindingMetadataProvider
{
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        var containerType = context.Key.ContainerType;
        if (containerType == null ||
            (!typeof(UpdateParentDto).IsAssignableFrom(containerType) &&
             !typeof(UpdateStudentDto).IsAssignableFrom(containerType))) return;

        var messages = context.BindingMetadata.ModelBindingMessageProvider ??= new DefaultModelBindingMessageProvider();
        messages.SetAttemptedValueIsInvalidAccessor((_, field) => $"Giá trị của {field} không hợp lệ.");
        messages.SetUnknownValueIsInvalidAccessor(field => $"Giá trị của {field} không hợp lệ.");
        messages.SetValueIsInvalidAccessor(_ => "Giá trị đã nhập không hợp lệ.");
    }
}
