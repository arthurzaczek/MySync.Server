﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using WebMatrix.WebData;
using MySync.Server.Filters;
using MySync.Server.Models;
using System.IO;
using System.Web.Configuration;
using System.Configuration;
using MySync.Server.DataProfile;
using System.Collections.Specialized;
using System.Reflection;

namespace MySync.Server.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class AccountController : Controller
    {
        //
        // GET: /Account/Setup

        [AllowAnonymous]
        public ActionResult Setup(string returnUrl)
        {
            // existing DataProfile Classes from web.config file
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            Dictionary<string, string> dpClasses = new Dictionary<string, string>();
            List<string> dpKeys = appSettings.AllKeys.Where(x => x.StartsWith("DP")).ToList();
            foreach (string key in dpKeys) dpClasses.Add(key, appSettings.Get(key));
            ViewBag.DPClasses = dpClasses;

            HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            ViewBag.MaxFileSize = section.MaxRequestLength / 1024 / 1000;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Setup

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Setup(SetupModel model, string returnUrl)
        {
            // If we got this far, something failed, redisplay form
            HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            int maxFilesize = section.MaxRequestLength / 1024 /1000;
            ViewBag.MaxFileSize = maxFilesize;
            ViewBag.DiskSpace = "123 Mb";

            // existing DataProfile Classes from web.config file
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            Dictionary<string, string> dpClasses = new Dictionary<string, string>();
            List<string> dpKeys = appSettings.AllKeys.Where(x => x.StartsWith("DP")).ToList();
            foreach (string key in dpKeys) dpClasses.Add(key, appSettings.Get(key));
            ViewBag.DPClasses = dpClasses;

            //check all given values from the view
            if (model.FileSize < 0 || model.FileSize > maxFilesize)
            {
                ModelState.AddModelError("", "Please correct all errors to save your settings.");
            }
            return View(model);
        }

        //
        // POST: /Account/Upload

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Upload(FormCollection formCollection)
        {
            if (Request != null)
            {                
                HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;

                string className = "DPFileSystem";
                DataProfile.DataProfile newContent = (DataProfile.DataProfile)Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType("MySync.Server.DataProfile." + className, true, true));
                newContent.SetSection(Server, Request, section);
                newContent.SaveFile();
            }

            return RedirectToAction("Setup", "Account");
        }

        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password))
            {
                return RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            WebSecurity.Logout();
            return RedirectToAction("Index", "Home");
        }

        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion
    }
}
