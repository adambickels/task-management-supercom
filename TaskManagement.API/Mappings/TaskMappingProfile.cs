using AutoMapper;
using TaskManagement.Core.DTOs;
using TaskManagement.Core.Entities;

namespace TaskManagement.API.Mappings
{
    /// <summary>
    /// AutoMapper profile for mapping between entities and DTOs
    /// </summary>
    public class TaskMappingProfile : Profile
    {
        public TaskMappingProfile()
        {
            // TaskItem to TaskItemDto
            CreateMap<TaskItem, TaskItemDto>()
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.TaskItemTags.Select(tt => tt.Tag)))
                .ForMember(dest => dest.TagIds, opt => opt.MapFrom(src => src.TaskItemTags.Select(tt => tt.TagId).ToList()));

            // TaskItemDto to TaskItem
            CreateMap<TaskItemDto, TaskItem>()
                .ForMember(dest => dest.TaskItemTags, opt => opt.Ignore()); // Handle tags separately in controller

            // Tag mappings
            CreateMap<Tag, TagDto>().ReverseMap();
        }
    }
}
