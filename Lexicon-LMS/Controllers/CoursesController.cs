  using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lexicon_LMS.Core.Entities;
using Lexicon_LMS.Data;
using Microsoft.AspNetCore.Authorization;
using Lexicon_LMS.Core.Entities.ViewModel;
using Lexicon_LMS.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;

namespace Lexicon_LMS.Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private readonly Lexicon_LMSContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CoursesController(IWebHostEnvironment webHostEnvironment, Lexicon_LMSContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;                                                                             
            _webHostEnvironment = webHostEnvironment;                                                                                                                                                                                                                           
        }

        // GET: Courses
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Index()
        {
              return _context.Course != null ? 
                          View(await _context.Course.ToListAsync()) :
                          Problem("Entity set 'Lexicon_LMSContext.Course'  is null.");
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Course == null)
            {
                return NotFound();
            }

            var course = await _context.Course
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        [Authorize(Roles = "Teacher")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.        
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CourseName,Description,StartDate,EndDate")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Courses/Edit/5
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Course == null)
            {
                return NotFound();
            }

            var course = await _context.Course.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseName,Description,StartDate,EndDate")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Courses/Delete/5
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Course == null)
            {
                return NotFound();
            }

            var course = await _context.Course
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [Authorize(Roles = "Teacher")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Course == null)
            {
                return Problem("Entity set 'Lexicon_LMSContext.Course'  is null.");
            }

            var course = await _context.Course
                .Include(u=> u.Users)
                .Include(u => u.Documents)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course != null)
            {
                _context.Course.Remove(course);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> CourseInfo(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var current = await CurrentCourse(id);
            var currentCourse = current.course;

            var assignmentList = await AssignmentListTeacher(id);
            var moduleList = await GetModuleListAsync(id);
            var module = moduleList.Find(y => y.IsCurrentModule);
            var Moduleactivity = new ModuleActivitiesViewModel();
            var documentList = new List<ActivityListViewModel>();


            if (module != null)
                Moduleactivity = await GetModuleActivityListAsync(module.Id);

            if(Moduleactivity.ActivityList == null)
            {
                Moduleactivity.ActivityList = new List<ActivityListViewModel>();
            }

            var model = new TeacherViewModel
            {
                Current = current,
                ModuleList = moduleList,
                ModulesActivity = Moduleactivity,
                AssignmentList = assignmentList,
                DocumentList = documentList,
                CourseId = currentCourse.Id,

            };

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }
        public async Task<CurrentViewModel> CurrentCourse(int? id)
        {
            var userId = _userManager.GetUserId(User);
            var course = await _context.Course.Include(a => a.Users)
                 .Include(a => a.Modules)
                .ThenInclude(a => a.Activities)
                .FirstOrDefaultAsync(a => a.Id == id);

            var students = course.Users.Count();

            var assignments = await _context.Activity.Where(c => c.ActivityType.ActivityTypeName == "Assignment" && c.Module.CourseId == id)
              .OrderBy(a => a.StartDate)
              .Select(a => new AssignmentsViewModel
              {
                  Id = a.Id,
                  Name = a.ActivityName,
                  DueTime = a.EndDate,
                  Finished = a.Documents.Where(d => d.IsFinished.Equals(true)).Count() * 100 / students
              })
              .ToListAsync();
            var model = new CurrentViewModel
            {
                course = course,
                Assignments = assignments
            };

            return model;
        }
        public async Task<List<AssignmentListViewModel>> AssignmentListTeacher(int? id)
        {
            var students = _context.Course.Find(id);


            var assignments = await _context.Activity
                .Where(a => a.ActivityType.ActivityTypeName == "Assignment" && a.Module.CourseId == id)
                .Select(a => new AssignmentListViewModel
                {
                    Id = a.Id,
                    Name = a.ActivityName,
                    StartDate = a.StartDate,
                    DateEndDate = a.EndDate,
                    CourseId = a.Module.CourseId,
                    ModuleId = a.Module.Id,
                    ModuleName = a.Module.ModulName,
                    ActivityId = a.Id,
                    Documents = a.Documents
                })
                .ToListAsync();

            return assignments;
        }
        public async Task<List<ModuleViewModel>> GetModuleListAsync(int? id)
        {
            var timeNow = DateTime.Now;

            var modules = await _context.Module.Include(a => a.Course)
                .Where(a => a.Course.Id == id)
                .Select(a => new ModuleViewModel
                {
                    Id = a.Id,
                    Name = a.ModulName,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    IsCurrentModule = false
                })

                .OrderBy(m => m.StartDate)
                .ToListAsync();
            if(modules.Count() > 0)
            {
                var currentModuleId = modules.OrderBy(t => Math.Abs((t.StartDate - timeNow).Ticks)).First().Id;

                SetCurrentModule(modules, currentModuleId);
            }
            else
            {
                modules = new List<ModuleViewModel>();
                
            }
            

            return modules;
        }


        private async Task<ModuleActivitiesViewModel> GetModuleActivityListAsync(int id)
        {

            ModuleActivitiesViewModel model = new ModuleActivitiesViewModel();

            model.ActivityList = await _context.Activity
                .Include(a => a.ActivityType)
                .Include(a => a.Documents)
                .Where(a => a.Module.Id == id)
                .OrderBy(a => a.StartDate)
                .Select(a => new ActivityListViewModel
                {
                    Id = a.Id,
                    ActivityName = a.ActivityName,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    ActivityTypeActivityTypeName = a.ActivityType.ActivityTypeName,
                    Documents = a.Documents,
                    CourseId = a.Module.CourseId,
                    ModuleId = a.ModuleId
                })
                .ToListAsync();

            if(model.ActivityList == null)
            {
                model.ActivityList = new List<ActivityListViewModel>();
            }

            model.ModuleId = id;
             

            return model;
        }
        public async Task<IActionResult> GetTeacherActivityAjax(int? id)
        {
            if (id == null) return BadRequest();

            if (Request.IsAjax())
            {
                var module = await _context.Module.FirstOrDefaultAsync(m => m.Id == id);

                if (module is null) return BadRequest();

                var modules = await _context.Module
                    .Where(m => m.CourseId == module.CourseId)
                    .OrderBy(m => m.StartDate)
                    .Select(m => new ModuleViewModel
                    {
                        Id = m.Id,
                        Name = m.ModulName,
                        StartDate = m.StartDate,
                        EndDate = m.EndDate,
                        IsCurrentModule = false

                    })
                   .ToListAsync();

                SetCurrentModule(modules, (int)id);


                var teacherModel = new TeacherViewModel()
                {
                    ModuleList = modules,
                    ModulesActivity = GetModuleActivityListAsync((int)id).Result,
                    CourseId = module.CourseId,  
                  
                };

                return PartialView("ModuleAndActivityPartial", teacherModel);
            }

            return BadRequest();
        }
        [HttpPost]
        public async Task<IActionResult> FileUpload([Bind(Prefix = "item")] AssignmentListViewModel viewModel)
        {

            var fullPath = await UploadFile(viewModel);
            var document = new Core.Entities.Document()
            {
                DocumentName = viewModel.UploadedFile.FileName,
                FilePath = fullPath,
                ActivityId = viewModel.ActivityId
            };

            _context.Add(document);
            await _context.SaveChangesAsync();

            TempData["msg"] = "File uploaded successfully";
            return LocalRedirect("~/User/WelcomePage");

        }
        public async Task<string> UploadFile([Bind(Prefix = "item")] AssignmentListViewModel viewModel)
        {
            var courseName = _context.Course.FirstOrDefault(c => c.Id == viewModel.CourseId)?.CourseName;
            var moduleName = _context.Module.FirstOrDefault(c => c.Id == viewModel.ModuleId)?.ModulName;
            var activityName = _context.Activity.FirstOrDefault(c => c.Id == viewModel.ActivityId)?.ActivityName;


            var PathToFile = Path.Combine(courseName, moduleName, activityName);
            var path = Path.Combine(_webHostEnvironment.WebRootPath, PathToFile);


            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);

            }

            string fileName = Path.GetFileName(viewModel.UploadedFile.FileName);

            var fullPath = Path.Combine(path, fileName);
            using (FileStream FileStream = new FileStream(fullPath, FileMode.Create))
            {
                viewModel.UploadedFile.CopyTo(FileStream);
            }

            var savePath = Path.Combine(PathToFile, fileName);
            return savePath;
        }

        [HttpGet]
        public IActionResult DownloadFile(string filepath)
        {
            var fileName = Path.GetFileName(filepath);
            var path = Path.Combine(_webHostEnvironment.WebRootPath, filepath);
            var fs = System.IO.File.ReadAllBytes(path);

            return File(fs, "application/octet-stream", fileName);
        }
        private bool CourseExists(int id)
        {
          return (_context.Course?.Any(e => e.Id == id)).GetValueOrDefault();
        }
        private List<ModuleViewModel> SetCurrentModule(List<ModuleViewModel> modules, int currentModuleId)
        {
            foreach (var module in modules)
            {
                if (module.Id == currentModuleId)
                {
                    module.IsCurrentModule = true;
                }
                else
                {
                    module.IsCurrentModule = false;
                }
            }

            return modules;
        }
    }
}
