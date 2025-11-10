# DotNetApp - Aplicación de Blog

## Descripción

DotNetApp es una aplicación web completa de blogging construida con .NET Core para el backend y Blazor para el frontend. Incluye funcionalidades de autenticación de usuarios, gestión de publicaciones, categorías y sistema de likes.

## Arquitectura

La aplicación está estructurada en varios proyectos:

- **BlogApi**: API RESTful que proporciona los endpoints para la gestión de usuarios, publicaciones, categorías y likes.
- **BlogData**: Proyecto de datos que contiene el contexto de Entity Framework, modelos y migraciones de base de datos.
- **BlogWeb**: Interfaz de usuario construida con Blazor Server.

## Tecnologías Utilizadas

- **Backend**: ASP.NET Core Web API
- **Frontend**: Blazor Server
- **Base de Datos**: SQLite (configurable para otros proveedores)
- **Autenticación**: JWT (JSON Web Tokens) con ASP.NET Core Identity
- **ORM**: Entity Framework Core
- **Contenedorización**: Docker y Docker Compose

## Requisitos Previos

- .NET 8.0 o superior
- Docker (opcional, para contenedorización)
- SQLite (incluido con .NET)

## Instalación y Configuración

### Opción 1: Usando Docker

1. Clona el repositorio:
   ```bash
   git clone <url-del-repositorio>
   cd DotNetApp
   ```

2. Ejecuta con Docker Compose:
   ```bash
   docker-compose up --build
   ```

### Opción 2: Ejecución Local

1. Clona el repositorio y navega al directorio:
   ```bash
   git clone <url-del-repositorio>
   cd DotNetApp
   ```

2. Restaura las dependencias:
   ```bash
   dotnet restore
   ```

3. Aplica las migraciones de base de datos:
   ```bash
   cd BlogApi
   dotnet ef database update
   cd ..
   ```

4. Ejecuta la aplicación:
   ```bash
   # API
   cd BlogApi
   dotnet run

   # Web (en otra terminal)
   cd BlogWeb
   dotnet run
   ```

## Configuración

### Configuración de JWT

La configuración de JWT se encuentra en `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong",
    "Issuer": "BlogApi",
    "Audience": "BlogApp"
  }
}
```

### Cadena de Conexión de Base de Datos

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=blog.db"
  }
}
```

## API Endpoints

### Autenticación

- `POST /api/auth/register` - Registrar nuevo usuario
- `POST /api/auth/login` - Iniciar sesión
- `POST /api/auth/logout` - Cerrar sesión
- `GET /api/auth/me` - Obtener información del usuario actual

### Publicaciones

- `GET /api/posts` - Obtener todas las publicaciones (con búsqueda opcional)
- `GET /api/posts/{id}` - Obtener publicación específica
- `POST /api/posts` - Crear nueva publicación (requiere autenticación)
- `PUT /api/posts/{id}` - Actualizar publicación
- `DELETE /api/posts/{id}` - Eliminar publicación

### Categorías

- `GET /api/categories` - Obtener todas las categorías
- `GET /api/categories/{id}` - Obtener categoría específica
- `POST /api/categories` - Crear nueva categoría (requiere autenticación)
- `PUT /api/categories/{id}` - Actualizar categoría (requiere autenticación)
- `DELETE /api/categories/{id}` - Eliminar categoría (requiere autenticación)

### Likes

- `GET /api/likes/post/{postId}` - Obtener likes de una publicación
- `POST /api/likes/post/{postId}` - Dar like a una publicación (requiere autenticación)
- `DELETE /api/likes/post/{postId}` - Quitar like de una publicación (requiere autenticación)
- `GET /api/likes/post/{postId}/status` - Verificar si el usuario dio like (requiere autenticación)

## Modelos de Datos

### Usuario (IdentityUser)
- Id: Identificador único
- UserName: Nombre de usuario
- Email: Correo electrónico

### Publicación (Post)
- Id: Identificador único
- Title: Título (3-100 caracteres)
- Content: Contenido (10-5000 caracteres)
- CreatedAt: Fecha de creación
- UpdatedAt: Fecha de actualización (opcional)
- AuthorId: ID del autor
- Author: Relación con el usuario autor
- Likes: Colección de likes
- LikesCount: Conteo computado de likes

### Categoría (Category)
- Id: Identificador único
- Name: Nombre (2-50 caracteres)
- Description: Descripción opcional (máximo 200 caracteres)
- CreatedAt: Fecha de creación
- Posts: Colección de publicaciones relacionadas

### Like
- Id: Identificador único
- PostId: ID de la publicación
- UserId: ID del usuario
- CreatedAt: Fecha de creación
- Post: Relación con la publicación
- User: Relación con el usuario

## Validaciones

### Registro de Usuario
- UserName: 2-50 caracteres, solo letras y números
- Password: 3-100 caracteres, debe contener mayúsculas, minúsculas, dígitos y caracteres especiales

### Creación de Publicación
- Title: 3-100 caracteres, no vacío
- Content: 10-5000 caracteres, no vacío

### Creación de Categoría
- Name: 2-50 caracteres, no vacío, único
- Description: Máximo 200 caracteres (opcional)

## Seguridad

- Autenticación JWT con tokens que expiran en 7 días
- Endpoints protegidos requieren el header `Authorization: Bearer <token>`
- Validación de entrada en todos los endpoints
- Prevención de likes en publicaciones propias

## Contenedorización

La aplicación incluye archivos Docker para facilitar el despliegue:

- `Dockerfile.api`: Para el contenedor de la API
- `Dockerfile.web`: Para el contenedor del frontend
- `docker-compose.yml`: Orquestación de servicios
- `nginx.conf`: Configuración del proxy reverso

## Desarrollo

### Estructura del Proyecto

```
DotNetApp/
├── BlogApi/           # API REST
│   ├── Controllers/   # Controladores de API
│   ├── Program.cs     # Punto de entrada
│   └── appsettings.json
├── BlogData/          # Capa de datos
│   ├── Context/       # Contexto de EF
│   ├── Models/        # Modelos de entidad
│   └── Migrations/    # Migraciones de BD
├── BlogWeb/           # Frontend Blazor
│   ├── Pages/         # Páginas Razor
│   ├── App.razor      # Layout principal
│   └── Program.cs     # Punto de entrada
└── docker-compose.yml # Configuración Docker
```

### Migraciones de Base de Datos

Para crear nuevas migraciones:
```bash
cd BlogData
dotnet ef migrations add <NombreMigracion>
dotnet ef database update
```

## Despliegue

### Usando Docker Compose

```bash
docker-compose up -d
```

### Despliegue Manual

1. Publica la API:
   ```bash
   cd BlogApi
   dotnet publish -c Release -o ./publish
   ```

2. Publica el frontend:
   ```bash
   cd BlogWeb
   dotnet publish -c Release -o ./publish
   ```

3. Configura un servidor web (IIS, Nginx, Apache) para servir los archivos publicados.

## Contribución

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## Licencia

Este proyecto está bajo la Licencia MIT. Ver el archivo `LICENSE` para más detalles.

## Soporte

Para soporte o preguntas, por favor abre un issue en el repositorio del proyecto.
