using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace RestServer.Controllers
{
    public class ProductsController : ApiController
    {
        /// <summary>
        /// GET all products
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage Get()
        {
            using (var db = new Models.DatabaseEntities())
            {
                var prods = (from p in db.products
                             select new { p.productName, p.productId, p.buyPrice, p.description, p.quantityInStock, p.productVendor }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, prods.AsEnumerable(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }


        /// <summary>
        /// GET by product name
        /// </summary>
        /// <param name="productName"></param>
        /// <returns></returns>
        public HttpResponseMessage Get(string productName)
        {

            using (var db = new Models.DatabaseEntities())
            {
                var prod = (from p in db.products
                            where p.productName == productName
                            select new { p.productName, p.buyPrice, p.description, p.quantityInStock, p.productVendor }).ToList();

                if (prod.Count() > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, prod, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Product not found", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
        }


        /// <summary>
        /// Get by productId
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public HttpResponseMessage Get(int productId)
        {

            using (var db = new Models.DatabaseEntities())
            {
                var prod = (from p in db.products
                            where p.productId == productId
                            select new { p.productName, p.buyPrice, p.description, p.quantityInStock, p.productVendor }).ToList();

                if (prod.Count() > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, prod, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Product not found", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
        }




        /// <summary>
        /// Post Method.  Uses parameters from query string.
        /// </summary>
        /// <param name="productName"></param>
        /// <param name="buyPrice"></param>
        /// <param name="quantityInStock"></param>
        /// <param name="productVendor"></param>
        /// <param name="description"></param>
        /// <param name="MSRP"></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post(string productName, decimal buyPrice, short quantityInStock, string productVendor = "", string description = "", decimal MSRP = 0)
        {
            using (var db = new Models.DatabaseEntities())
            {
                var prod = new Models.product();
                prod.productName = productName;
                prod.buyPrice = buyPrice;
                prod.description = description;
                prod.quantityInStock = quantityInStock;
                prod.productVendor = productVendor;
                prod.MSRP = MSRP;
                db.products.Add(prod);

                try
                {
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ex.EntityValidationErrors.ToList(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }

                var msg = Request.CreateResponse(HttpStatusCode.Created, "Product " + prod.productName + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                msg.Headers.Location = new Uri(Request.RequestUri + "&productId=" + prod.productId.ToString());
                return msg;
            }
        }


        /// <summary>
        /// POST.  Create new product by passing Json object. 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post([FromBody]JObject json)
        {
            using (var db = new Models.DatabaseEntities())
            {
                Models.product prod = JsonConvert.DeserializeObject<Models.product>(json.ToString());

                try
                {
                    db.products.Add(prod);
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // A required field was most likely missing from the json object
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ex.EntityValidationErrors.ToList(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                catch (Exception ex)
                {
                    // Something like log4net could be implemented to write out the actual errors and info to a file
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "An error occurred and product was not created. Error message: " + ex.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }


                var msg = Request.CreateResponse(HttpStatusCode.Created, "Product " + prod.productName + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                msg.Headers.Location = new Uri(Request.RequestUri + "&productId=" + prod.productId.ToString());
                return msg;
            }
        }


        /// <summary>
        /// PUT.  Updates a product.  
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPut]
        public HttpResponseMessage Put([FromBody]JArray json)
        {
            using (var db = new Models.DatabaseEntities())
            {
                List<Models.product> prods = JsonConvert.DeserializeObject<List<Models.product>>(json.ToString());

                foreach (Models.product prod in prods)
                {
                    Models.product existingProduct = db.products.FirstOrDefault(product => product.productId == prod.productId);
                    if (existingProduct != null)
                    {
                        existingProduct.productName = prod.productName == null ? existingProduct.productName : prod.productName;
                        existingProduct.productVendor = prod.productVendor == null ? existingProduct.productVendor : prod.productVendor;
                        existingProduct.quantityInStock = prod.quantityInStock == null ? existingProduct.quantityInStock : prod.quantityInStock;
                        existingProduct.description = prod.description == null ? existingProduct.description : prod.description;
                        existingProduct.buyPrice = prod.buyPrice == null ? existingProduct.buyPrice : prod.buyPrice;
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No product with productId " + prod.productId + " was found.  No order was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                }
                try
                {
                    //db.products.Attach(prod);
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException ex)
                {
                    // Something like log4net could be implemented to write out the actual errors and info to a file
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "An error occurred and product was not updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // A required field was most likely missing from the json object
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ex.EntityValidationErrors.ToList(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                catch (Exception ex)
                {
                    // Something like log4net could be implemented to write out the actual errors and info to a file
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "An error occurred and product was not updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }

                return Request.CreateResponse(HttpStatusCode.OK, "Products were updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }


        /// <summary>
        /// Delete method
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        [HttpDelete]
        public HttpResponseMessage Delete(decimal productId)
        {
            using (var db = new Models.DatabaseEntities())
            {
                Models.product prod = db.products.FirstOrDefault(p => p.productId == productId);

                if (prod != null)
                {
                    db.products.Attach(prod);
                    db.products.Remove(prod);
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException ex)
                    {
                        // Something like log4net could be implemented to write out the actual errors and info to a file
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred and product was not deleted.  Error Message " + ex.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                    catch (Exception ex)
                    {
                        // Something like log4net could be implemented to write out the actual errors and info to a file
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred and product was not deleted.  Error Message " + ex.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, "Product " + prod.productName + " with Product Code " + productId + " was deleted.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No product found with Product Code " + productId + ".", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
        }
    }
}
