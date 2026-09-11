namespace Invoyz.Api.Application.PdfJobs;

public sealed record PdfDownload(Stream Content, string FileName);
