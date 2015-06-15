using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BasicRestService.Controllers
{
    public class AccountController : ApiController
    {

        /// <summary>
        /// Will return json array of all users.
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage Get()
        {
            using (var db = new Models.DatabaseEntities())
            {
                var allUsers = (from c in db.users
                                select new { c.userNumber, c.userFirstName, c.userLastName, c.name, c.phone, c.addressLine1, c.city, c.state, c.postalCode, c.country, c.email }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, allUsers.AsEnumerable(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }


        /// <summary>
        ///  Get request to ask for information about a specific user (user)
        /// </summary>
        /// <param name="userNumber"></param>
        /// <returns></returns>
        public HttpResponseMessage Get(decimal userNumber)
        {
            using (var db = new Models.DatabaseEntities())
            {
                var userInfo = (from c in db.users
                                where c.userNumber == userNumber
                                select new { c.userNumber, c.userFirstName, c.userLastName, c.name, c.phone, c.addressLine1, c.city, c.state, c.postalCode, c.country, c.email }).ToList();


                if (userInfo.Count() > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, userInfo, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                else
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No User Info found");
                }
            }
        }



        /// <summary>
        ///  POST.  User passes individual parameters.
        /// </summary>
        /// <param name="userFirstName"></param>
        /// <param name="userLastName"></param>
        /// <param name="phone"></param>
        /// <param name="addressLine1"></param>
        /// <param name="city"></param>
        /// <param name="state"></param>
        /// <param name="postalCode"></param>
        /// <param name="country"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post(string userFirstName, string userLastName, decimal phone, string addressLine1, string city, string state, string postalCode, string country, string email)
        {
            using (var db = new Models.DatabaseEntities())
            {

                Models.user newUser = new Models.user();
                newUser.userFirstName = userFirstName;
                newUser.userLastName = userLastName;
                newUser.name = userFirstName + " " + userLastName;
                newUser.phone = phone;
                newUser.addressLine1 = addressLine1;
                newUser.city = city;
                newUser.state = state;
                newUser.postalCode = postalCode;
                newUser.country = country;
                newUser.email = email;

                db.users.Add(newUser);

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

                var msg = Request.CreateResponse(HttpStatusCode.Created, "User " + newUser.name + " with id " + newUser.userNumber + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                msg.Headers.Location = new Uri(Request.RequestUri + "&userId=" + newUser.userNumber.ToString());
                return msg;
            }
        }


        /// <summary>
        /// POST.  This will create a new user.  Does not support multiple Accounts at once.  
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post([FromBody]JObject json)
        {
            using (var db = new Models.DatabaseEntities())
            {
                Models.user newUser = JsonConvert.DeserializeObject<Models.user>(json.ToString());

                newUser.name = newUser.userFirstName + " " + newUser.userLastName;
                db.users.Add(newUser);

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

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }

                var msg = Request.CreateResponse(HttpStatusCode.Created, "User " + newUser.name + " with id " + newUser.userNumber + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                msg.Headers.Location = new Uri(Request.RequestUri + "&userId=" + newUser.userNumber.ToString());
                return msg;
            }
        }


        /// <summary>
        /// Put.  This is to update a user
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPut]
        public HttpResponseMessage Put([FromBody]JObject json)
        {
            using (var db = new Models.DatabaseEntities())
            {
                Models.user user = JsonConvert.DeserializeObject<Models.user>(json.ToString());
                Models.user existingUser = db.users.FirstOrDefault(u => u.userNumber == user.userNumber);  // See if the usernumber exists.  If it doesn't it is to add a new user.  If it does, it is to update a user

                // could use a comparer here
                existingUser.userFirstName = user.userFirstName == null ? existingUser.userFirstName : user.userFirstName;
                existingUser.userLastName = user.userLastName == null ? existingUser.userLastName : user.userLastName;
                existingUser.phone = user.phone == null ? existingUser.phone : user.phone;
                existingUser.addressLine1 = user.addressLine1 == null ? existingUser.addressLine1 : user.addressLine1;
                existingUser.city = user.city == null ? existingUser.city : user.city;
                existingUser.state = user.state == null ? existingUser.state : user.state;
                existingUser.postalCode = user.postalCode == null ? existingUser.postalCode : user.postalCode;
                existingUser.country = user.country == null ? existingUser.country : user.country;
                existingUser.email = user.email == null ? existingUser.email : user.email;

                // TODO See what the other exceptions would be thrown
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "User Information was not updated." + " Error Message:" + ex.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }

                return Request.CreateResponse(HttpStatusCode.OK, "User Information was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }



        /// <summary>
        /// Put method
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="acctJson"></param>
        /// <returns></returns>
        [HttpPut]
        public HttpResponseMessage Put(bool batch, [FromBody]JArray acctJson)
        {
            if (batch)
            {
                using (var db = new Models.DatabaseEntities())
                {
                    //List<Models.user> users = JsonConvert.DeserializeObject<List<Models.user>>(acctJson.ToString());


                    foreach (var user in acctJson)  // Doing it this way instead of using the users above becuase the username and password that get passed with it aren't part of the user class
                    {
                        decimal userNumber = Convert.ToDecimal(user["userNumber"].ToString());
                        Models.user existingUser = db.users.FirstOrDefault(u => u.userNumber == userNumber);  // See if the usernumber exists.  If it doesn't it is to add a new user.  If it does, it is to update a user                         

                        // could use a comparer here
                        existingUser.userFirstName = user["userFirstName"].ToString() == null ? existingUser.userFirstName : user["userFirstName"].ToString();
                        existingUser.userLastName = user["userLastName"].ToString() == null ? existingUser.userLastName : user["userLastName"].ToString();
                        existingUser.phone = Convert.ToDecimal(user["phone"].ToString()) == null ? existingUser.phone : Convert.ToDecimal(user["phone"].ToString());
                        existingUser.addressLine1 = user["addressLine1"].ToString() == null ? existingUser.addressLine1 : user["addressLine1"].ToString();
                        existingUser.city = user["city"].ToString() == null ? existingUser.city : user["city"].ToString();
                        existingUser.state = user["state"].ToString() == null ? existingUser.state : user["state"].ToString();
                        existingUser.postalCode = user["postalCode"].ToString() == null ? existingUser.postalCode : user["postalCode"].ToString();
                        existingUser.country = user["country"].ToString() == null ? existingUser.country : user["country"].ToString();
                        existingUser.email = user["email"].ToString() == null ? existingUser.email : user["email"].ToString();

                    }

                    // TODO See what the other exceptions would be thrown
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, "User Information was not updated." + " Error Message:" + ex.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, "User Information was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }



        /// <summary>
        /// Delete.  This will delete a user.  The username is unique so it can be used to find and delete an existing user.  
        /// </summary>
        /// <param name="userNumber"></param>
        /// <returns></returns>
        public HttpResponseMessage Delete(decimal userNumber)
        {
            using (var db = new Models.DatabaseEntities())
            {
                try
                {
                    Models.user existingUser = db.users.FirstOrDefault(u => u.userNumber == userNumber);

                    db.users.Remove(existingUser);

                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, "User was deleted.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error has occurred (user not found or other error) and the request was not completed." + " Error Message: " + ex.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
        }
    }
}
