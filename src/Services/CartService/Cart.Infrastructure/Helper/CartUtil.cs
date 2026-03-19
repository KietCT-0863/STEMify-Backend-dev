using Cart.Application.Models;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text;

namespace Cart.Infrastructure.Helper
{
    public class CartUtil
    {
        public static Dictionary<int, CartItemDTO> GetCartFromCookie(string cookieValue)
        {
            Dictionary<int, CartItemDTO> cart = new Dictionary<int, CartItemDTO>();
            string decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(cookieValue));
            string[] itemsList = decodedString.Split('|');

            foreach (string strItem in itemsList)
            {
                if (!string.IsNullOrEmpty(strItem))
                {
                    string[] arrItemDetail = strItem.Split(',');
                    int itemId = int.Parse(arrItemDetail[0].Trim());
                    int quantity = arrItemDetail.Length > 1 ? int.Parse(arrItemDetail[1].Trim()) : 1;

                    CartItemDTO item = new CartItemDTO()
                    {
                        ItemId = itemId,
                        Quantity = quantity
                    };
                    cart[itemId] = item;
                }
            }
            return cart;
        }

        public static string ConvertCartToString(List<CartItemDTO> itemsList)
        {
            StringBuilder strItemsInCart = new StringBuilder();
            foreach (CartItemDTO item in itemsList)
            {
                strItemsInCart.Append($"{item.ItemId},{item.Quantity}|");
            }
            string encodedString = Convert.ToBase64String(Encoding.UTF8.GetBytes(strItemsInCart.ToString()));
            return encodedString;
        }

        public static Cookie GetCookieByName(HttpRequest request, string cookieName)
        {
            if (request.Cookies.TryGetValue(cookieName, out string cookieValue))
            {
                return new Cookie(cookieName, cookieValue);
            }
            return null;
        }

        public static void SaveCartToCookie(HttpRequest request, HttpResponse response, string strItemsInCart, string? userId)
        {
            string cookieName = string.IsNullOrEmpty(userId) ? "Cart" : "Cart_" + userId;
            CookieOptions options = new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true
            };
            response.Cookies.Append(cookieName, strItemsInCart, options);
        }

        public static void DeleteCartToCookie(HttpRequest request, HttpResponse response, string? userId)
        {
            string cookieName = string.IsNullOrEmpty(userId) ? "Cart" : "Cart_" + userId;
            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(-1),
                HttpOnly = true,
                IsEssential = true
            };

            response.Cookies.Delete(cookieName, options);
        }

        public static List<string> CookieNames(HttpRequest request)
        {
            return request.Cookies.Keys.ToList();
        }
    }
}
