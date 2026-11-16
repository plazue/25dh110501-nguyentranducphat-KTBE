using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BE1.Models;
using BE1.Models.ViewModel;

namespace BE1.Areas.Admin.Controllers
{
    public class ProductsController : Controller
    {
        private MyStoreEntities1 db = new MyStoreEntities1();

        // GET: Admin/Products
        public ActionResult Index(string searchTerm, decimal? minPrice, decimal? maxPrice)
        {
            var model = new SearchProductVM();

           
            model.SearchTerm = searchTerm;
            model.MinPrice = minPrice;
            model.MaxPrice = maxPrice;

            var products = db.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.ProductName.Contains(searchTerm) ||
                                               p.ProductDecription.Contains(searchTerm) ||
                                               p.Category.CategoryName.Contains(searchTerm));
            }

            // Lọc theo giá tối thiểu
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice >= minPrice.Value);
            }

            // Lọc theo giá tối đa
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice <= maxPrice.Value);
            }

            model.Products = products.ToList();

            return View(model);
        }

        // GET: Admin/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh chính
                if (product.UploadImg != null && product.UploadImg.ContentLength > 0)
                {
                    string fileName = System.IO.Path.GetFileName(product.UploadImg.FileName);
                    string path = System.IO.Path.Combine(Server.MapPath("~/Images"), fileName);
                    product.UploadImg.SaveAs(path);
                    product.ProductImage = "/Images/" + fileName;
                }

               
                // Xử lý nhiều ảnh phụ (UploadImg1)
                if (product.UploadImg1 != null && product.UploadImg1.Any())
                {
                    var listPaths = new List<string>();
                    foreach (var file in product.UploadImg1)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(file.FileName);
                            string path = Path.Combine(Server.MapPath("~/Images"), fileName);
                            file.SaveAs(path);
                            listPaths.Add("/Images/" + fileName);
                        }
                    }
                    if (listPaths.Any())
                    {
                        product.ProductImage1 = string.Join(",", listPaths); 
                    }
                }

                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                var existingProduct = db.Products.AsNoTracking().FirstOrDefault(p => p.ProductID == product.ProductID);

                // Xử lý ảnh chính
                if (product.UploadImg != null && product.UploadImg.ContentLength > 0)
                {
                    string fileName = System.IO.Path.GetFileName(product.UploadImg.FileName);
                    string path = System.IO.Path.Combine(Server.MapPath("~/Images"), fileName);
                    product.UploadImg.SaveAs(path);
                    product.ProductImage = "/Images/" + fileName;
                }
                else
                {
                    if (existingProduct != null)
                    {
                        product.ProductImage = existingProduct.ProductImage;
                    }
                }

                // Xử lý ảnh phụ (ProductImage1)
                // Xử lý nhiều ảnh phụ (UploadImg1)
                if (product.UploadImg1 != null && product.UploadImg1.Any())
                {
                    var listPaths = new List<string>();
                    foreach (var file in product.UploadImg1)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(file.FileName);
                            string path = Path.Combine(Server.MapPath("~/Images"), fileName);
                            file.SaveAs(path);
                            listPaths.Add("/Images/" + fileName);
                        }
                    }
                    if (listPaths.Any())
                    {
                        product.ProductImage1 = string.Join(",", listPaths); // Lưu chuỗi: "/Images/img1.jpg,/Images/img2.jpg"
                    }
                }

                db.Entry(product).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}