using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resource.Application.Common.Interfaces;
using Resource.Application.Models.ExportData;
using Resource.Application.Queries.Exporter;
using Resource.Application.Specifications.Courses;
using Shared.Helper;
using Shared.Protos.Resource;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Resource.Application.Handlers.Exporter
{
    public class GetExportedCourseHandler : IRequestHandler<GetExportedCourse, ExportCourseResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ILogger<GetExportedCourseHandler> _logger;
        private readonly IConfiguration _configuration;

        public GetExportedCourseHandler(IResourceUnitOfWork unitOfWork, ILogger<GetExportedCourseHandler> logger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ExportCourseResponse> Handle(
            GetExportedCourse request,
            CancellationToken cancellationToken
        )
        {
            var stream = await ExportCourseStreamAsync(request.Id);
            var fileName = $"course-{request.Id}.rsa";

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            var zipData = Google.Protobuf.ByteString.CopyFrom(bytes);
            return new ExportCourseResponse
            {
                ZipData = zipData,
                Filename = fileName,
                Size = zipData.Length
            };
        }

        public async Task<Stream> ExportCourseStreamAsync(int courseId)
        {
            var course = await GetCourseWithDetailsAsync(courseId);
            if (course == null)
            {
                throw new ArgumentException($"Course with ID {courseId} not found");
            }

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                await CreateManifestAsync(archive, course);
                await CreateLessonsAsync(archive, course);
                await CreateConfigAsync(archive);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        private async Task<CourseExportModel?> GetCourseWithDetailsAsync(int courseId)
        {
            var spec = new CourseDetailByIdSpecification(courseId);
            var course = await _unitOfWork.Courses.FirstOrDefaultAsync(spec);

            if (course == null)
                throw new KeyNotFoundException($"Course with ID {courseId} not found.");

            var result = new CourseExportModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Level = course.Level.ToString(),
                Duration = course.Duration,
                CreatedDate = course.CreatedDate,
                Topics = course.Lessons.SelectMany(l => l.LessonTopics).Select(t => t.Topic.Name.Trim()).ToList() ?? new List<string>(),
                Lessons = course.Lessons
                    //.Where(l => l.Status == Domain.Enums.LessonStatus.Published)
                    .Where(l =>
                        l.Status != Domain.Enums.LessonStatus.Archived &&
                        l.Status != Domain.Enums.LessonStatus.Deleted
                    )
                    .OrderBy(l => l.OrderIndex)
                    .Select(l => new LessonExportModel
                    {
                        Id = l.Id,
                        Level = course.Level.ToString(),
                        Topics = l.LessonTopics?.Select(t => t.Topic.Name.Trim()).ToList() ?? new List<string>(),
                        Title = l.Title,
                        Description = l.Description,
                        LearningOutcome = l.LearningOutcome,
                        Duration = l.Duration,
                        CreatedDate = l.CreatedDate,
                        Assets = l.LessonAssets
                            .Select(a => new LessonAssetExportModel
                            {
                                Id = a.Id,
                                Name = a.Name,
                                Type = a.Type,
                                AssetUrl = a.AssetUrl,
                                Format = a.Format,
                                Size = a.Size
                            }).ToList(),
                        Sections = l.Sections
                            .OrderBy(s => s.OrderIndex)
                            .Select(s => new SectionExportModel
                            {
                                Id = s.Id,
                                Title = s.Title,
                                Description = s.Description,
                                OrderIndex = s.OrderIndex,
                                Duration = s.Duration,
                                Contents = s.Contents
                                    .Where(c =>
                                        c.Status != Domain.Enums.ContentStatus.Archived &&
                                        c.Status != Domain.Enums.ContentStatus.Deleted
                                    )
                                    .OrderBy(c => c.Id)
                                    .Select(c => new ContentExportModel
                                    {
                                        Id = c.Id,
                                        ContentType = c.ContentType.ToString(),
                                        ContentBody = c.ContentBody,
                                        FileName = c.FileName,
                                        FileUrl = c.FileUrl
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
                    }).ToList()
            };

            return result;
        }

        private async Task CreateManifestAsync(ZipArchive archive, CourseExportModel course)
        {
            try
            {
                var lessons = new List<LessonManifest>();
                var assets = new AssetsManifest();

                List<string> tags = course.Topics ?? new List<string>();
                string difficulty = course.Level ?? "";

                // Create lesson manifests instead of slide manifests
                foreach (var lesson in course.Lessons)
                {
                    var totalSlides = 0;
                    foreach (var section in lesson.Sections)
                    {
                        totalSlides += section.Contents.Count + section.Quizzes.Count;
                    }

                    var lessonManifest = new LessonManifest
                    {
                        Id = $"lesson-{lesson.Id}",
                        Title = lesson.Title,
                        Description = lesson.Description,
                        Folder = $"lessons/lesson-{lesson.Id}", // Relative to course/
                        Duration = lesson.Duration,
                        TotalSlides = totalSlides,
                        Tags = lesson.Topics.Any() ? lesson.Topics : tags,
                        Difficulty = difficulty,
                        LearningOutcome = lesson.LearningOutcome,
                        HasMedia = await CheckLessonHasMedia(lesson)
                    };

                    lessons.Add(lessonManifest);
                }

                // Extract assets from all lessons
                assets = await ExtractCourseAssetsAsync(course);

                var manifest = new CourseManifestModel
                {
                    Id = $"course-{course.Id}",
                    Title = course.Title,
                    Description = course.Description,
                    Author = "STEM Education Team",
                    Version = "1.0.0",
                    CreatedAt = course.CreatedDate.UtcDateTime,
                    LastModified = DateTime.UtcNow,
                    TotalLessons = lessons.Count,
                    Duration = course.Duration,
                    Lessons = lessons,
                    Assets = assets,
                    Metadata = new CourseMetadata
                    {
                        Language = "en",
                        Subject = "STEM",
                        Grade = "K-12",
                        Level = course.Level,
                        Keywords = ExtractCourseKeywords(course),
                        License = "CC-BY-4.0",
                        CustomMetadata = new Dictionary<string, object>
                        {
                            { "originalCourseId", course.Id },
                            { "exportedAt", DateTime.UtcNow }
                        }
                    }
                };

                // Create manifest at course/manifest.json (RSA-ultimate requirement)
                var manifestEntry = archive.CreateEntry("course/manifest.json");
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

        private async Task<bool> CheckLessonHasMedia(LessonExportModel lesson)
        {
            foreach (var section in lesson.Sections)
            {
                foreach (var content in section.Contents)
                {
                    if (RSAExportHelper.HasMediaContent(content.ContentBody))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<AssetsManifest> ExtractCourseAssetsAsync(CourseExportModel course)
        {
            var assets = new AssetsManifest();

            foreach (var lesson in course.Lessons)
            {
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
                                Path = $"course/assets/images/{fileName}", // Updated path
                                Type = "image",
                                Size = await RSAExportHelper.GetFileSizeAsync(imageUrl),
                                MimeType = RSAExportHelper.GetMimeType(imageUrl)
                            };

                            // Avoid duplicates
                            if (!assets.Images.Any(a => a.Name == fileName))
                            {
                                assets.Images.Add(assetItem);
                            }
                        }

                        // Process video URLs
                        foreach (var videoUrl in videoUrls)
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(videoUrl);
                            var assetItem = new AssetItem
                            {
                                Id = $"asset-{Guid.NewGuid():N}",
                                Name = fileName,
                                Path = $"course/assets/video/{fileName}", // Updated path
                                Type = "video",
                                Size = await RSAExportHelper.GetFileSizeAsync(videoUrl),
                                MimeType = RSAExportHelper.GetMimeType(videoUrl)
                            };

                            if (!assets.Video.Any(a => a.Name == fileName))
                            {
                                assets.Video.Add(assetItem);
                            }
                        }

                        // Process audio URLs
                        foreach (var audioUrl in audioUrls)
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(audioUrl);
                            var assetItem = new AssetItem
                            {
                                Id = $"asset-{Guid.NewGuid():N}",
                                Name = fileName,
                                Path = $"course/assets/audio/{fileName}", // Updated path
                                Type = "audio",
                                Size = await RSAExportHelper.GetFileSizeAsync(audioUrl),
                                MimeType = RSAExportHelper.GetMimeType(audioUrl)
                            };

                            if (!assets.Audio.Any(a => a.Name == fileName))
                            {
                                assets.Audio.Add(assetItem);
                            }
                        }
                    }
                }
            }

            return assets;
        }

        private List<string> ExtractCourseKeywords(CourseExportModel course)
        {
            var keywords = new List<string> { "STEM", "science", "technology", "engineering", "mathematics", "course" };

            // Extract from title and description
            var text = $"{course.Title} {course.Description}".ToLower();

            if (text.Contains("robot")) keywords.Add("robotics");
            if (text.Contains("programming") || text.Contains("code")) keywords.Add("programming");
            if (text.Contains("arduino")) keywords.Add("arduino");
            if (text.Contains("sensor")) keywords.Add("sensors");

            // Add lesson-specific keywords
            foreach (var lesson in course.Lessons)
            {
                var lessonText = $"{lesson.Title} {lesson.Description} {lesson.LearningOutcome}".ToLower();
                if (lessonText.Contains("experiment")) keywords.Add("experiment");
                if (lessonText.Contains("project")) keywords.Add("project");
            }

            return keywords.Distinct().ToList();
        }

        private async Task CreateLessonsAsync(ZipArchive archive, CourseExportModel course)
        {
            try
            {
                // Create course root directory (RSA-ultimate requirement)
                archive.CreateEntry("course/");
                
                // Create shared assets directories under course/
                archive.CreateEntry("course/assets/");
                archive.CreateEntry("course/assets/images/");
                archive.CreateEntry("course/assets/audio/");
                archive.CreateEntry("course/assets/video/");
                archive.CreateEntry("course/assets/slides/");
                archive.CreateEntry("course/assets/documents/");

                // Copy all unique assets first
                await CopyCourseAssetsToArchiveAsync(archive, course);

                // Create lessons folder structure under course/
                archive.CreateEntry("course/lessons/");
                foreach (var lesson in course.Lessons)
                {
                    var lessonFolder = $"course/lessons/lesson-{lesson.Id}/slides/";

                    // Create lesson folder
                    archive.CreateEntry($"course/lessons/lesson-{lesson.Id}/");
                    archive.CreateEntry(lessonFolder);

                    var slideIndex = 1;
                    foreach (var section in lesson.Sections)
                    {
                        // Process contents as HTML slides
                        foreach (var content in section.Contents)
                        {
                            var fileName = $"{slideIndex:000}.html";
                            var contentEntry = archive.CreateEntry($"{lessonFolder}{fileName}");

                            using var contentStream = contentEntry.Open();
                            using var writer = new StreamWriter(contentStream, Encoding.UTF8);

                            var htmlContent = await GenerateHtmlContentAsync(content, lesson.Title, isInCourse: true);
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
                                    Answers = q.Answers.Select(a => new AnswerModel
                                    {
                                        AnswerId = a.Id,
                                        Content = a.Content,
                                        IsCorrect = a.IsCorrect
                                    }).ToList()
                                }).ToList()
                            };

                            var quizEntry = archive.CreateEntry($"{lessonFolder}quiz-{quiz.Id}.json");
                            using var quizStream = quizEntry.Open();
                            var jsonOptions = new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            };
                            await JsonSerializer.SerializeAsync(quizStream, quizModel, jsonOptions);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to create lessons: {ex.Message}", ex);
            }
        }

        private async Task<string> GenerateHtmlContentAsync(ContentExportModel content, string lessonTitle, bool isInCourse = false)
        {
            var contentBody = content.ContentBody;

            // Replace Cloudinary URLs with local asset paths
            contentBody = await RSAExportHelper.ReplaceUrlsWithLocalPathsForCourse(contentBody, isInCourse);

            // CSS path relative to slide location: course/lessons/lesson-X/slides/001.html
            // Need to go up 4 levels: ../../../config/style.css
            var cssPath = "../../../config/style.css";

            var template = $@"<!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>{lessonTitle} - Content {content.Id}</title>
                <link rel=""stylesheet"" href=""{cssPath}"">
            </head>
            <body>
                <div class=""content-container"">
                    <h1 class=""content-title"">{lessonTitle}</h1>
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

        private async Task CopyCourseAssetsToArchiveAsync(ZipArchive archive, CourseExportModel course)
        {
            var processedAssets = new HashSet<string>();

            _logger.LogInformation("=== START CopyCourseAssetsToArchiveAsync ===");
            _logger.LogInformation("Course ID: {CourseId}, Total Lessons: {LessonCount}", course.Id, course.Lessons.Count);

            foreach (var lesson in course.Lessons)
            {
                _logger.LogInformation("Processing Lesson {LessonId}: {LessonTitle}", lesson.Id, lesson.Title);
                
                foreach (var section in lesson.Sections)
                {
                    _logger.LogInformation("  Section {SectionId}: {SectionTitle}, Contents: {ContentCount}", 
                        section.Id, section.Title, section.Contents.Count);
                    
                    foreach (var content in section.Contents)
                    {
                        _logger.LogInformation("    Content {ContentId}, Type: {ContentType}, BodyLength: {BodyLength}", 
                            content.Id, content.ContentType, content.ContentBody?.Length ?? 0);
                        
                        // Log FileUrl if exists
                        if (!string.IsNullOrEmpty(content.FileUrl))
                        {
                            _logger.LogInformation("      ⚠️ Content has FileUrl: {FileUrl}, FileName: {FileName}", 
                                content.FileUrl, content.FileName ?? "N/A");
                        }
                        
                        // Extract and copy images from HTML content
                        var imageUrls = RSAExportHelper.ExtractImageUrlsFromHtml(content.ContentBody);
                        _logger.LogInformation("      Found {ImageCount} images", imageUrls.Count);
                        
                        foreach (var imageUrl in imageUrls)
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(imageUrl);
                            if (!processedAssets.Contains(fileName))
                            {
                                try
                                {
                                    // Assets now under course/assets/
                                    var assetPath = $"course/assets/images/{fileName}";
                                    _logger.LogInformation("      Downloading image: {ImageUrl} -> {AssetPath}", imageUrl, assetPath);
                                    await RSAExportHelper.CopyFileToArchiveAsync(archive, imageUrl, assetPath);
                                    processedAssets.Add(fileName);
                                    _logger.LogInformation("      ✓ Downloaded successfully");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to copy image asset {imageUrl} to archive", imageUrl);
                                }
                            }
                        }

                        // Extract and copy videos from HTML content
                        var videoUrls = RSAExportHelper.ExtractVideoUrlsFromHtml(content.ContentBody);
                        foreach (var videoUrl in videoUrls)
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(videoUrl);
                            if (!processedAssets.Contains(fileName))
                            {
                                try
                                {
                                    // Assets now under course/assets/
                                    var assetPath = $"course/assets/video/{fileName}";
                                    await RSAExportHelper.CopyFileToArchiveAsync(archive, videoUrl, assetPath);
                                    processedAssets.Add(fileName);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Failed to copy video asset {videoUrl} to archive");
                                }
                            }
                        }

                        // Extract and copy audio from HTML content
                        var audioUrls = RSAExportHelper.ExtractAudioUrlsFromHtml(content.ContentBody);
                        foreach (var audioUrl in audioUrls)
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(audioUrl);
                            if (!processedAssets.Contains(fileName))
                            {
                                try
                                {
                                    // Assets now under course/assets/
                                    var assetPath = $"course/assets/audio/{fileName}";
                                    await RSAExportHelper.CopyFileToArchiveAsync(archive, audioUrl, assetPath);
                                    processedAssets.Add(fileName);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Failed to copy audio asset {audioUrl} to archive");
                                }
                            }
                        }

                        // Download PPTX/Document files from FileUrl if exists
                        if (!string.IsNullOrEmpty(content.FileUrl))
                        {
                            var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(content.FileUrl);
                            if (!processedAssets.Contains(fileName))
                            {
                                try
                                {
                                    var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                                    string assetPath;
                                    
                                    // Determine asset folder based on file extension
                                    if (fileExtension == ".pptx" || fileExtension == ".ppt")
                                    {
                                        assetPath = $"course/assets/slides/{fileName}";
                                        _logger.LogInformation("      📊 Downloading PPTX: {FileUrl} -> {AssetPath}", content.FileUrl, assetPath);
                                    }
                                    else if (fileExtension == ".pdf")
                                    {
                                        assetPath = $"course/assets/documents/{fileName}";
                                        _logger.LogInformation("      📄 Downloading PDF: {FileUrl} -> {AssetPath}", content.FileUrl, assetPath);
                                    }
                                    else
                                    {
                                        assetPath = $"course/assets/documents/{fileName}";
                                        _logger.LogInformation("      📎 Downloading file: {FileUrl} -> {AssetPath}", content.FileUrl, assetPath);
                                    }
                                    
                                    await RSAExportHelper.CopyFileToArchiveAsync(archive, content.FileUrl, assetPath);
                                    processedAssets.Add(fileName);
                                    _logger.LogInformation("      ✓ Downloaded successfully");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to copy file from FileUrl {FileUrl} to archive", content.FileUrl);
                                }
                            }
                        }
                    }
                }
                
                // Download LessonAssets (PPTX, PDF, etc.)
                _logger.LogInformation("  Processing {AssetCount} LessonAssets", lesson.Assets.Count);
                foreach (var asset in lesson.Assets)
                {
                    var fileName = RSAExportHelper.GetFileNameFromCloudinaryUrl(asset.AssetUrl);
                    if (!processedAssets.Contains(fileName))
                    {
                        try
                        {
                            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                            string assetPath;
                            
                            // Determine asset folder based on file type
                            if (fileExtension == ".pptx" || fileExtension == ".ppt")
                            {
                                assetPath = $"course/assets/slides/{fileName}";
                                _logger.LogInformation("    📊 Downloading PPTX: {AssetUrl} -> {AssetPath}", asset.AssetUrl, assetPath);
                            }
                            else if (fileExtension == ".pdf")
                            {
                                assetPath = $"course/assets/documents/{fileName}";
                                _logger.LogInformation("    📄 Downloading PDF: {AssetUrl} -> {AssetPath}", asset.AssetUrl, assetPath);
                            }
                            else if (fileExtension == ".mp4" || fileExtension == ".avi" || fileExtension == ".mov")
                            {
                                assetPath = $"course/assets/video/{fileName}";
                                _logger.LogInformation("    🎥 Downloading Video: {AssetUrl} -> {AssetPath}", asset.AssetUrl, assetPath);
                            }
                            else
                            {
                                assetPath = $"course/assets/documents/{fileName}";
                                _logger.LogInformation("    📎 Downloading file: {AssetUrl} -> {AssetPath}", asset.AssetUrl, assetPath);
                            }
                            
                            await RSAExportHelper.CopyFileToArchiveAsync(archive, asset.AssetUrl, assetPath);
                            processedAssets.Add(fileName);
                            _logger.LogInformation("    ✓ Downloaded successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to copy LessonAsset {AssetUrl} to archive", asset.AssetUrl);
                        }
                    }
                }
            }
            
            _logger.LogInformation("=== END CopyCourseAssetsToArchiveAsync ===");
            _logger.LogInformation("Total assets processed: {AssetCount}", processedAssets.Count);
        }

        private async Task CreateConfigAsync(ZipArchive archive)
        {
            var cssContent = await GetExportCssContentAsync();

            // Create config under course/ directory (RSA-ultimate requirement)
            archive.CreateEntry("course/config/");
            var styleEntry = archive.CreateEntry("course/config/style.css");
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

            return fileCss + customCss;
        }
    }
}