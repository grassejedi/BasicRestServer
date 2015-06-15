using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using BasicRestService.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BasicRestService.Controllers
{
    public class OrderController : ApiController
    {
        private DatabaseEntities db = new DatabaseEntities();

        /// <summary>
        /// Get all orders. 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage Get()
        {
            var orders = (from o in db.orders
                          select new
                          {
                              o.orderNumber,
                              o.orderDate,
                              o.requiredDate,
                              o.shippedDate,
                              o.status,
                              o.comments,
                              orderTotal = (from od in db.orderdetails
                                            where od.orderNumber == o.orderNumber
                                            select od.total).Sum(),
                              orderDetails = (from od in db.orderdetails
                                              where od.orderNumber == o.orderNumber
                                              select new { od.product.productName, od.product.productVendor, od.product.description, od.quantityOrdered, od.product.buyPrice, od.total })
                          }).ToList();

            if (orders.Count() > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK, orders, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "No Orders found");
            }
        }


        /// <summary>
        /// Get by userName.  
        /// </summary>
        /// <param name="userNumber"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage Get(decimal userNumber)
        {
            Models.user user = db.users.FirstOrDefault(l => l.userNumber == userNumber);

            if (user != null)
            {
                var orders = (from o in db.orders
                              where o.userNumber == user.userNumber
                              select new
                              {
                                  o.orderNumber,
                                  o.orderDate,
                                  o.requiredDate,
                                  o.shippedDate,
                                  o.status,
                                  o.comments,
                                  orderTotal = (from od in db.orderdetails
                                                where od.orderNumber == o.orderNumber
                                                select od.total).Sum(),
                                  orderDetails = (from od in db.orderdetails
                                                  where od.orderNumber == o.orderNumber
                                                  select new { od.product.productName, od.product.productVendor, od.product.description, od.quantityOrdered, od.product.buyPrice, od.total })
                              }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, orders, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "No user found.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }



        //TODO:  Setup a GET that will take a orderNumber

        /// <summary>
        /// Post order and order detail as json object
        /// ex:
        ///     {
        //    "orderDate":  "Mon, 06 Apr 2015",
        //    "requiredDate": "Wed, 08 Apr 2015",
        //    "comments": "Test comment",
        //    "userNumber": 100001,
        //    "orderDetails": [{"productId": 10000000
        //                  "quantityOrdered": -30},
        //                 {"productId": 10000001,
        //                  "quantityOrdered": 30}]  
        //      }
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post([FromBody]JObject json)
        {
            Models.order newOrder;
            // TODO:  Put try catch on all json conversion
            try
            {
                newOrder = JsonConvert.DeserializeObject<Models.order>(json.ToString());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid Json object was recieved.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }

            Models.user existingUser = db.users.FirstOrDefault(u => u.userNumber == newOrder.userNumber);

            newOrder.orderDate = (newOrder.orderDate == default(DateTime)) ? DateTime.Now : newOrder.orderDate;

            if (json["orderDetails"] == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "No Products were found with the Order.  Order was not created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }

            if (newOrder.orderdetails.Count == 0)
            {
                foreach (var od in json["orderDetails"])
                {
                    Models.orderdetail newOd = new Models.orderdetail();

                    try
                    {
                        newOd.productId = Convert.ToDecimal(od["productId"]);
                        newOd.quantityOrdered = Convert.ToInt32(od["quantityOrdered"]);
                    }
                    catch (Exception ex)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid Json object was for OrderDetail.  It should be subArry of Order and should include productId and quantityOrdered.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }

                    Models.product prod = db.products.FirstOrDefault(p => p.productId == Convert.ToDecimal(od["productId"]));  // Need to get access to the actual product
                    if (prod != null)
                    {
                        newOd.total = newOd.quantityOrdered * prod.buyPrice;

                        if (newOd.quantityOrdered > prod.quantityInStock)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, "Quantity requested exceeds available product.  Quantity avaiable: " + prod.quantityInStock + ".", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid productId found.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }

                    if (newOd.quantityOrdered == 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Quantity must be greater than 0.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                }
            }
            else
            {

                foreach (Models.orderdetail od in newOrder.orderdetails)
                {
                    Models.product prod = db.products.FirstOrDefault(p => p.productId == od.productId);  // Need to get access to the actual product
                    if (prod != null)
                    {
                        od.total = od.quantityOrdered * prod.buyPrice;

                        if (od.quantityOrdered > prod.quantityInStock)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, "Quantity requested exceeds available product.  Quantity avaiable: " + prod.quantityInStock + ".", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid productId found.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }

                    if (od.quantityOrdered == 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Quantity must be greater than 0.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                }
            }

            db.orders.Add(newOrder);

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
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }


            var msg = Request.CreateResponse(HttpStatusCode.Created, "Order with id " + newOrder.orderNumber + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            msg.Headers.Location = new Uri(Request.RequestUri + "&orderNumber=" + newOrder.orderNumber.ToString());
            return msg;
        }



        /// <summary>
        /// PUT method. Because of the complexity, I chose to just support updating the overall Order and not the Order Details
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPut]
        public HttpResponseMessage Put([FromBody]JArray json)
        {
            List<order> orders = JsonConvert.DeserializeObject<List<order>>(json.ToString());

            foreach (order o in orders)
            {
                order existingOrder = db.orders.FirstOrDefault(ord => ord.orderNumber == o.orderNumber);
                if (existingOrder != null)
                {
                    existingOrder.orderDate = o.orderDate == null ? existingOrder.orderDate : o.orderDate;
                    existingOrder.requiredDate = o.requiredDate == null ? existingOrder.requiredDate : o.requiredDate;
                    existingOrder.shippedDate = o.shippedDate == null ? existingOrder.shippedDate : o.shippedDate;
                    existingOrder.status = o.status == null ? existingOrder.status : o.status;
                    existingOrder.comments = o.comments == null ? existingOrder.comments : o.comments;
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No order with order number " + o.orderNumber + " was found.  No order was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }

            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Order was not updated. " + ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Order was not updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }

            return Request.CreateResponse(HttpStatusCode.OK, "Order was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
        }


        /// <summary>
        /// DELETE method
        /// </summary>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        [HttpDelete]
        public HttpResponseMessage Delete(int orderNumber)
        {
            using (var db = new Models.DatabaseEntities())
            {
                order order = db.orders.FirstOrDefault(o => o.orderNumber == orderNumber);
                if (order == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Not Found.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                else
                {

                    List<orderdetail> ordDetails = db.orderdetails.Where(o => o.order.orderNumber == orderNumber).ToList();
                    //TODO: update the rest of this

                    foreach (orderdetail ord in ordDetails)
                    {
                        db.orderdetails.Remove(ord);
                    }

                    db.orders.Remove(order);

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "An Error occurred and the order was not deleted.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                    catch (Exception ex)
                    {
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, "An Error occurred and the order was not deleted.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, "Order " + orderNumber + " was deleted", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
        }
    }
}