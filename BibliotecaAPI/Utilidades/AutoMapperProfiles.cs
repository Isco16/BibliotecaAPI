using AutoMapper;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Utilidades
{
    public class AutoMapperProfiles: Profile
    {
        public AutoMapperProfiles() // El constructor tiene que ser publico.
        {
            CreateMap<LlaveAPI, LlaveDTO>();
            CreateMap<RestriccionDominio, RestriccionDominioDTO>();
            CreateMap<RestriccionIP, RestriccionIPDTO>();

            // ForMember es para mapear una propiedad del DTO a partir de una propiedad del modelo, en este caso, NombreCompleto se mapea a partir de Nombres y Apellidos del modelo Autor, utilizando el método MapearNombreYApellido.
            CreateMap<Autor, AutorDTO>().ForMember(dto => dto.NombreCompleto, config => config.MapFrom(autor => MapearNombreYApellido(autor)));

            CreateMap<Autor, AutorConLibrosDTO>().ForMember(dto => dto.NombreCompleto, config => config.MapFrom(autor => MapearNombreYApellido(autor)));

            CreateMap<AutorCreacionDTO, Autor>();
            CreateMap<AutorCreacionDTO, Autor>().ForMember(ent => ent.Foto, config => config.Ignore());
            CreateMap<Autor, AutorPatchDTO>().ReverseMap(); // ReverseMap es para mapear en ambos sentidos, de Autor a AutorPatchDTO y de AutorPatchDTO a Autor.

            // El mapeo de AutorLibro a LibroDTO es para mapear las propiedades Id y Titulo del DTO a partir de las propiedades LibroId y Libro.Titulo del modelo AutorLibro.
            CreateMap<AutorLibro, LibroDTO>()
                .ForMember(dto => dto.Id, config => config.MapFrom(ent => ent.LibroId))
                .ForMember(dto => dto.Titulo, config => config.MapFrom(ent => ent.Libro!.Titulo));

            CreateMap<Libro, LibroDTO>();            
            CreateMap<LibroCreacionDTO, Libro>()
                .ForMember(libro => libro.Autores, config => 
                    config.MapFrom(dto => dto.AutoresIds.Select(id => new AutorLibro { AutorId = id })));

            CreateMap<Libro, LibroConAutoresDTO>();

            // El mapeo de AutorLibro a AutorDTO es para mapear las propiedades Id y NombreCompleto del DTO a partir de las propiedades AutorId y el resultado del método MapearNombreYApellido aplicado a la propiedad Autor del modelo AutorLibro.
            CreateMap<AutorLibro, AutorDTO>()
                .ForMember(dto => dto.Id, config => config.MapFrom(ent => ent.AutorId))
                .ForMember(dto => dto.NombreCompleto, config => config.MapFrom(ent => MapearNombreYApellido(ent.Autor!)));

            CreateMap<LibroCreacionDTO, AutorLibro>()
                .ForMember(ent => ent.Libro, config => config.MapFrom(dto => new Libro { Titulo = dto.Titulo }));

            CreateMap<ComentarioCreacionDTO, Comentario>();
            CreateMap<Comentario, ComentarioDTO>()
                .ForMember(dto => dto.UsuarioEmail, config => config.MapFrom(dto => dto.Usuario!.Email));
            CreateMap<Comentario, ComentarioPatchDTO>().ReverseMap();

            CreateMap<Usuario, UsuarioDTO>();
        }

        private string MapearNombreYApellido(Autor autor) => $"{autor.Nombres} {autor.Apellidos}";
    }
}
