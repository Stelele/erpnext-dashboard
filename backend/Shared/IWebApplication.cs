using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Shared;

public interface IWebApplication : IHost, IApplicationBuilder, IEndpointRouteBuilder;
