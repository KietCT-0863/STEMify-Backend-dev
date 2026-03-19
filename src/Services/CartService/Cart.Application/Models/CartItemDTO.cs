namespace Cart.Application.Models
{
    public class CartItemDTO
    {
        public int ItemId { get; set; }

        public int Quantity { get; set; }
    }

    public class CartProductInformation
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string ImageURL { get; set; }
        public string Description { get; set; }
    }

    public class InstructorInformation
    {
        public string Id { get; set; }
        public string Fullname { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

    public class CartItemInformation
    {
        public CartProductInformation CartCourseInformation { get; set; }
        //public List<InstructorInformation> InstructorInformation { get; set; }
        //public ReviewInformation CourseReviewInformation { get; set; }
        //public DiscountInformation? DiscountInformation { get; set; }
        //public CouponInformation? CouponInformation { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CouponInformation
    {
        public string Code { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal CalculatedDiscount { get; private set; }
        public decimal CalculatedPrice { get; private set; }

        public void CalculateDiscountAndPrice(decimal price)
        {
            CalculatedDiscount = DiscountType == "Percentage" ? price * DiscountValue / 100 : DiscountValue;
            CalculatedPrice = price - CalculatedDiscount;
        }
    }

    public class DiscountInformation
    {
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal CalculatedDiscount { get; private set; }
        public decimal CalculatedPrice { get; private set; }

        public void CalculateDiscountAndPrice(decimal price)
        {
            CalculatedDiscount = DiscountType == "Percentage" ? price * DiscountValue / 100 : DiscountValue;
            CalculatedPrice = price - CalculatedDiscount;
        }
    }

    public class ReviewInformation
    {
        public decimal AverageRating { get; set; }
        public int TotalRatings { get; set; }
    }

    public class CartInformation
    {
        public List<CartItemInformation> CartItems { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
