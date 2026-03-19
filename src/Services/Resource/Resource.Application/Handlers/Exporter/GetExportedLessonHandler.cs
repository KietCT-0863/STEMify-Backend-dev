using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resource.Application.Common.Interfaces;
using Resource.Application.Models.ExportData;
using Resource.Application.Queries.Exporter;
using Resource.Application.Specifications.Lessons;
using Shared.Helper;
using Shared.Protos.Resource;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Resource.Application.Handlers.Exporter
{
    public class GetExportedLessonHandler : IRequestHandler<GetExportedLesson, ExportLessonResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ILogger<GetExportedLessonHandler> _logger;
        private readonly IConfiguration _configuration;

        public GetExportedLessonHandler(IResourceUnitOfWork unitOfWork, ILogger<GetExportedLessonHandler> logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ExportLessonResponse> Handle(
            GetExportedLesson request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var stream = await ExportLessonStreamAsync(request.Id);
                var fileName = $"lesson-{request.Id}.rsa";

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
                var zipData = Google.Protobuf.ByteString.CopyFrom(bytes);
                return new ExportLessonResponse
                {
                    ZipData = zipData,
                    Filename = fileName,
                    Size = zipData.Length
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the lesson: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<byte[]> ExportLessonAsync(int lessonId)
        {
            using var stream = await ExportLessonStreamAsync(lessonId);
            return ((MemoryStream)stream).ToArray();
        }

        public async Task<Stream> ExportLessonStreamAsync(int lessonId)
        {
            var lesson = await GetLessonWithDetailsAsync(lessonId);
            if (lesson == null)
            {
                throw new ArgumentException($"Lesson with ID {lessonId} not found");
            }

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                await CreateManifestAsync(archive, lesson);
                await CreateSectionsAsync(archive, lesson);
                await CreateConfigAsync(archive);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        private async Task<LessonExportModel?> GetLessonWithDetailsAsync(int lessonId)
        {
            var spec = new LessonDetailByIdSpecification(lessonId);
            var lesson = await _unitOfWork.Lessons.FirstOrDefaultAsync(spec);

            if (lesson == null)
                throw new KeyNotFoundException($"Lesson with ID {lessonId} not found.");

            // Project the lesson entity to an anonymous object
            var result = new LessonExportModel
            {
                Id = lesson.Id,
                Level = lesson.Course.Level.ToString(),
                Topics = lesson.LessonTopics.Select(t => t.Topic.Name.Trim()).ToList(),
                Title = lesson.Title,
                Description = lesson.Description,
                LearningOutcome = lesson.LearningOutcome,
                Duration = lesson.Duration,
                CreatedDate = lesson.CreatedDate,
                Sections = lesson.Sections
                        .OrderBy(s => s.OrderIndex)
                        .Select(s => new SectionExportModel
                        {
                            Id = s.Id,
                            Title = s.Title,
                            Description = s.Description,
                            OrderIndex = s.OrderIndex,
                            Duration = s.Duration,
                            Contents = s.Contents
                                //.Where(c => c.Status == Domain.Enums.ContentStatus.Published)
                                .Where(l =>
                                    l.Status != Domain.Enums.ContentStatus.Archived &&
                                    l.Status != Domain.Enums.ContentStatus.Deleted
                                )
                                .OrderByDescending(c => c.Id)
                                .Take(1)
                                .Select(c => new ContentExportModel
                                {
                                    Id = c.Id,
                                    ContentType = c.ContentType.ToString(),
                                    ContentBody = c.ContentBody,
                                    //FileName = c.FileName,
                                    //FileUrl = c.FileUrl
                                }).ToList()
                            //Quizzes = s.Quizzes
                            //    //.Where(q => q.Status == Domain.Enums.QuizStatus.Published)
                            //    .Where(l =>
                            //        l.Status != Domain.Enums.QuizStatus.Archived &&
                            //        l.Status != Domain.Enums.QuizStatus.Deleted
                            //    )
                            //    .OrderByDescending(c => c.Id)
                            //    .Take(1)
                            //    .Select(q => new QuizExportDataModel
                            //    {
                            //        Id = q.Id,
                            //        Title = q.Title,
                            //        TotalMarks = q.TotalMarks,
                            //        PassingMarks = q.PassingMarks,
                            //        Duration = q.Duration,
                            //        Questions = q.Questions
                            //            .OrderBy(qu => qu.OrderIndex)
                            //            .Select(qu => new QuestionExportModel
                            //            {
                            //                Id = qu.Id,
                            //                Content = qu.Content,
                            //                QuestionType = qu.QuestionType.ToString(),
                            //                OrderIndex = qu.OrderIndex,
                            //                Answers = qu.Answers.Select(a => new AnswerExportModel
                            //                {
                            //                    Id = a.Id,
                            //                    Content = a.Content,
                            //                    IsCorrect = a.IsCorrect
                            //                }).ToList()
                            //            }).ToList()
                            //    }).ToList()
                        }).ToList()
            };
            return result;
        }

        private async Task CreateManifestAsync(ZipArchive archive, LessonExportModel lesson)
        {
            try
            {
                var slideIndex = 1;
                var slides = new List<SlideManifest>();
                var assets = new AssetsManifest();

                List<string> tags = lesson.Topics ?? new List<string>();
                string difficulty = lesson.Level ?? "";

                foreach (var section in lesson.Sections)
                {
                    // Process contents as slides
                    foreach (var content in section.Contents)
                    {
                        var slideId = $"slide-{slideIndex:000}";
                        var fileName = $"{slideIndex:000}.html";

                        var slide = new SlideManifest
                        {
                            Id = slideId,
                            Title = section.Title,
                            Description = section.Description,
                            File = $"slides/{fileName}",
                            Type = content.ContentType.ToString(),
                            Duration = section.Duration / section.Contents.Count(), // Distribute section duration
                            Tags = tags,
                            Difficulty = difficulty,
                            HasQuiz = false,
                            HasAnnotation = true,
                            HasMedia = RSAExportHelper.HasMediaContent(content.ContentBody)
                        };

                        slides.Add(slide);
                        slideIndex++;
                    }

                    // Process quizzes as slides
                    foreach (var quiz in section.Quizzes)
                    {
                        var slideId = $"quiz-{quiz.Id}";
                        var fileName = $"quiz-{quiz.Id}.json";

                        var slide = new SlideManifest
                        {
                            Id = slideId,
                            Title = quiz.Title,
                            Description = quiz.Title,
                            File = $"slides/{fileName}",
                            Type = "quiz",
                            Duration = quiz.Duration,
                            Tags = new List<string> { "quiz", "assessment" },
                            Difficulty = "medium",
                            HasQuiz = true,
                            HasAnnotation = false,
                            HasMedia = false
                        };

                        slides.Add(slide);
                    }
                }

                // Extract assets from content
                assets = await ExtractAssetsAsync(lesson);

                var manifest = new ManifestModel
                {
                    Id = $"lesson-{lesson.Id}",
                    Title = lesson.Title,
                    Description = lesson.Description,
                    Author = "STEM Education Team",
                    Version = "1.0.0",
                    CreatedAt = lesson.CreatedDate.UtcDateTime,
                    LastModified = DateTime.UtcNow,
                    TotalSlides = slides.Count,
                    Slides = slides,
                    Assets = assets,
                    Metadata = new LessonMetadata
                    {
                        Language = "en",
                        Subject = "STEM",
                        Grade = "K-12",
                        Keywords = ExtractKeywords(lesson),
                        License = "CC-BY-4.0",
                        CustomMetadata = new Dictionary<string, object>
                    {
                        { "learningOutcome", lesson.LearningOutcome },
                        { "originalLessonId", lesson.Id },
                        { "exportedAt", DateTime.UtcNow }
                    }
                    }
                };

                var manifestEntry = archive.CreateEntry("manifest.json");
                using var manifestStream = manifestEntry.Open();
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                await JsonSerializer.SerializeAsync(manifestStream, manifest, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to create manifest: {ex.Message}", ex);
            }
        }

        private List<string> ExtractKeywords(LessonExportModel lesson)
        {
            var keywords = new List<string> { "STEM", "science", "technology", "engineering", "mathematics" };

            // Extract from title and description
            var text = $"{lesson.Title} {lesson.Description} {lesson.LearningOutcome}".ToLower();

            if (text.Contains("robot")) keywords.Add("robotics");
            if (text.Contains("programming") || text.Contains("code")) keywords.Add("programming");
            if (text.Contains("arduino")) keywords.Add("arduino");
            if (text.Contains("sensor")) keywords.Add("sensors");

            return keywords.Distinct().ToList();
        }

        private async Task<AssetsManifest> ExtractAssetsAsync(LessonExportModel lesson)
        {
            var assets = new AssetsManifest();

            foreach (var section in lesson.Sections)
            {
                foreach (var content in section.Contents)
                {
                    // Extract URLs from HTML content body
                    var imageUrls = RSAExportHelper.ExtractImageUrlsFromHtml(content.ContentBody);
                    var videoUrls = RSAExportHelper.ExtractVideoUrlsFromHtml(content.ContentBody);
                    var audioUrls = RSAExportHelper.ExtractAudioUrlsFromHtml(content.ContentBody);

                    // Process image URLs
                    foreach (var imageUrl in imageUrls)
                    {
                        var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(imageUrl);
                        var assetItem = new AssetItem
                        {
                            Id = $"asset-{Guid.NewGuid():N}",
                            Name = fileName,
                            Path = $"assets/images/{fileName}",
                            Type = "image",
                            Size = await RSAExportHelper.GetFileSizeAsync(imageUrl),
                            MimeType = RSAExportHelper.GetMimeType(imageUrl)
                        };
                        assets.Images.Add(assetItem);
                    }

                    // Process video URLs
                    foreach (var videoUrl in videoUrls)
                    {
                        var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(videoUrl);
                        var assetItem = new AssetItem
                        {
                            Id = $"asset-{Guid.NewGuid():N}",
                            Name = fileName,
                            Path = $"assets/video/{fileName}",
                            Type = "video",
                            Size = await RSAExportHelper.GetFileSizeAsync(videoUrl),
                            MimeType = RSAExportHelper.GetMimeType(videoUrl)
                        };
                        assets.Video.Add(assetItem);
                    }

                    // Process audio URLs
                    foreach (var audioUrl in audioUrls)
                    {
                        var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(audioUrl);
                        var assetItem = new AssetItem
                        {
                            Id = $"asset-{Guid.NewGuid():N}",
                            Name = fileName,
                            Path = $"assets/audio/{fileName}",
                            Type = "audio",
                            Size = await RSAExportHelper.GetFileSizeAsync(audioUrl),
                            MimeType = RSAExportHelper.GetMimeType(audioUrl)
                        };
                        assets.Audio.Add(assetItem);
                    }
                }
            }

            return assets;
        }

        private async Task CopyAssetsToArchiveAsync(ZipArchive archive, LessonExportModel lesson)
        {
            foreach (var section in lesson.Sections)
            {
                foreach (var content in section.Contents)
                {
                    // Extract and copy images from HTML content
                    var imageUrls = RSAExportHelper.ExtractImageUrlsFromHtml(content.ContentBody);
                    foreach (var imageUrl in imageUrls)
                    {
                        try
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(imageUrl);
                            var assetPath = $"assets/images/{fileName}";
                            await RSAExportHelper.CopyFileToArchiveAsync(archive, imageUrl, assetPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to copy image asset {imageUrl} to archive");
                        }
                    }

                    // Extract and copy videos from HTML content
                    var videoUrls = RSAExportHelper.ExtractVideoUrlsFromHtml(content.ContentBody);
                    foreach (var videoUrl in videoUrls)
                    {
                        try
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(videoUrl);
                            var assetPath = $"assets/video/{fileName}";
                            await RSAExportHelper.CopyFileToArchiveAsync(archive, videoUrl, assetPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to copy video asset {videoUrl} to archive");
                        }
                    }

                    // Extract and copy audio from HTML content
                    var audioUrls = RSAExportHelper.ExtractAudioUrlsFromHtml(content.ContentBody);
                    foreach (var audioUrl in audioUrls)
                    {
                        try
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(audioUrl);
                            var assetPath = $"assets/audio/{fileName}";
                            await RSAExportHelper.CopyFileToArchiveAsync(archive, audioUrl, assetPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to copy audio asset {audioUrl} to archive");
                        }
                    }
                }
            }
        }

        // Update the GenerateHtmlContentAsync method to use local asset paths:
        private async Task<string> GenerateHtmlContentAsync(ContentExportModel content, string title)
        {
            var contentBody = content.ContentBody;

            // Replace Cloudinary URLs with local asset paths
            contentBody = await RSAExportHelper.ReplaceUrlsWithLocalPaths(contentBody);

            var template = $@"<!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>{title} - Content {content.Id}</title>
                <link rel=""stylesheet"" href=""../config/style.css"">
            </head>
            <body>
                <div class=""content-container"">
                    <h1 class=""content-title"">{title}</h1>
                    <div class=""content-body"">
                        {contentBody}
                    </div>
                </div>
                <script>
                      document.querySelectorAll('[data-type=""step-block""]').forEach((block) => {{
                        let steps = [];
                        try {{
                          steps = JSON.parse(
                            decodeURIComponent(block.getAttribute('data-steps'))
                          );
                        }} catch (e) {{
                          console.error('Lỗi parse steps', e);
                        }}
                        let currentStep =
                          parseInt(block.getAttribute('data-current-step')) || 0;

                        const stepContainer = block.querySelector('.step-container');
                        const stepBlocks = stepContainer
                          ? [stepContainer]
                          : Array.from(block.querySelectorAll('.my-3.space-y-2'));

                        const navContainers = block.querySelectorAll('.step-nav');

                        const renderNavButtons = () => {{
                          navContainers.forEach((nav) => {{
                            nav.innerHTML = '';
                            steps.forEach((_, idx) => {{
                              const btn = document.createElement('button');
                              btn.className =
                                'step-index-btn h-6 w-6 rounded-full text-sm font-bold ' +
                                (idx === currentStep
                                  ? 'bg-black text-white'
                                  : 'bg-white text-black');
                              btn.textContent = idx + 1;
                              btn.addEventListener('click', () => {{
                                currentStep = idx;
                                block.setAttribute('data-current-step', String(currentStep));
                                renderStep();
                              }});
                              nav.appendChild(btn);
                            }});
                          }});
                        }};

                        const renderStep = () => {{
                          const step = steps[currentStep];
                          if (!step) return;

                          if (stepContainer) {{
                            // Trường hợp có step-container → render động
                            stepContainer.innerHTML = '';

                            const titleEl = document.createElement('h3');
                            titleEl.className = 'text-lg font-bold text-center';
                            titleEl.textContent = `${{currentStep + 1}}. ${{step.title || ''}}`;
                            stepContainer.appendChild(titleEl);

                            const imgs = Array.isArray(step.images)
                              ? step.images
                              : step.imageUrl
                              ? [step.imageUrl]
                              : [];
                            if (imgs.length) {{
                              const imgWrapper = document.createElement('div');
                              imgWrapper.className =
                                'flex flex-wrap items-center justify-center gap-5 my-3';
                              imgs.forEach((img, idx) => {{
                                const imgEl = document.createElement('img');
                                imgEl.src = img;
                                imgEl.alt = `${{step.title || 'step'}}-${{idx}}`;
                                imgEl.className =
                                  'aspect-auto w-[200px] h-[200px] rounded-2xl border object-contain';
                                imgWrapper.appendChild(imgEl);
                              }});
                              stepContainer.appendChild(imgWrapper);
                            }}

                            if (step.content) {{
                              const p = document.createElement('p');
                              p.className =
                                'mt-3 text-gray-700 whitespace-pre-line text-center';
                              p.textContent = String(step.content).replace(/n/g, 'n');
                              stepContainer.appendChild(p);
                            }}
                          }} else {{
                            // Trường hợp nhiều step tĩnh → ẩn/hiện
                            stepBlocks.forEach((el, idx) => {{
                              el.style.display = idx === currentStep ? '' : 'none';
                              // căn giữa cho title và content sẵn có
                              el.querySelectorAll('h3').forEach((h) =>
                                h.classList.add('text-center')
                              );
                              el.querySelectorAll('p').forEach((p) =>
                                p.classList.add('text-center')
                              );
                            }});
                          }}

                          renderNavButtons();
                        }};

                        // Prev / Next
                        block.querySelectorAll('[data-action=""prev""]').forEach((btn) =>
                          btn.addEventListener('click', () => {{
                            currentStep = (currentStep - 1 + steps.length) % steps.length;
                            block.setAttribute('data-current-step', String(currentStep));
                            renderStep();
                          }})
                        );
                        block.querySelectorAll('[data-action=""next""]').forEach((btn) =>
                          btn.addEventListener('click', () => {{
                            currentStep = (currentStep + 1) % steps.length;
                            block.setAttribute('data-current-step', String(currentStep));
                            renderStep();
                          }})
                        );

                        renderStep();
                      }});
                    </script>
            </body>
            </html>";
            return template;
        }

        private async Task CreateSectionsAsync(ZipArchive archive, LessonExportModel lesson)
        {
            try
            {

                var slideIndex = 1;
                var title = lesson.Title ?? "Lesson";
                foreach (var section in lesson.Sections)
                {
                    // Process contents as HTML slides
                    foreach (var content in section.Contents)
                    {
                        var fileName = $"{slideIndex:000}.html";
                        var contentEntry = archive.CreateEntry($"slides/{fileName}");

                        using var contentStream = contentEntry.Open();
                        using var writer = new StreamWriter(contentStream, Encoding.UTF8);

                        var htmlContent = await GenerateHtmlContentAsync(content, title);
                        await writer.WriteAsync(htmlContent);

                        slideIndex++;
                    }

                    // Process quizzes as JSON files
                    foreach (var quiz in section.Quizzes)
                    {
                        var quizModel = new QuizExportModel
                        {
                            QuizId = quiz.Id,
                            Title = quiz.Title,
                            TotalMarks = quiz.TotalMarks,
                            PassingMarks = quiz.PassingMarks,
                            Duration = quiz.Duration,
                            Questions = quiz.Questions.Select(q => new QuestionModel
                            {
                                QuestionId = q.Id,
                                Content = q.Content,
                                QuestionType = q.QuestionType.ToString(),
                                OrderIndex = q.OrderIndex,
                                Answers = (q.Answers).Select(a => new AnswerModel
                                {
                                    AnswerId = a.Id,
                                    Content = a.Content,
                                    IsCorrect = a.IsCorrect
                                }).ToList()
                            }).ToList()
                        };


                        var quizEntry = archive.CreateEntry($"slides/quiz-{quiz.Id}.json");
                        using var quizStream = quizEntry.Open();
                        var jsonOptions = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        await JsonSerializer.SerializeAsync(quizStream, quizModel, jsonOptions);
                    }
                }

                // Create assets directories with placeholder files
                archive.CreateEntry("assets/images/");
                archive.CreateEntry("assets/audio/");
                archive.CreateEntry("assets/video/");

                // TODO: Copy actual asset files based on the extracted assets from manifest
                await CopyAssetsToArchiveAsync(archive, lesson);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to create sections: {ex.Message}", ex);
            }
        }

        private async Task CreateConfigAsync(ZipArchive archive)
        {
            // Get the export.min.css content from your configuration or file system
            var cssContent = await GetExportCssContentAsync();

            var styleEntry = archive.CreateEntry("config/style.css");
            using var styleStream = styleEntry.Open();
            using var writer = new StreamWriter(styleStream, Encoding.UTF8);
            await writer.WriteAsync(cssContent);
        }

        private async Task<string> GetExportCssContentAsync()
        {
            var customCss = @"
                .content-container {
                    max-width: 1200px;
                    margin: 0 auto;
                    padding: 20px;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                }
        
                .content-body {
                    line-height: 1.6;
                    color: #333;
                }
        
                .content-body h1, .content-body h2, .content-body h3 {
                    margin-top: 2em;
                    margin-bottom: 1em;
                    color: #2c3e50;
                }
        
                .content-body p {
                    margin-bottom: 1em;
                }
        
                .content-body img {
                    max-width: 100%;
                    height: auto;
                }
            ";

            var cssPath = _configuration["ExportCssPath"] ?? "wwwroot/css/export.min.css";
            string fileCss = string.Empty;

            if (File.Exists(cssPath))
            {
                fileCss = await File.ReadAllTextAsync(cssPath);
            }

            // Always include custom CSS
            return fileCss + customCss;
        }
    }
}

