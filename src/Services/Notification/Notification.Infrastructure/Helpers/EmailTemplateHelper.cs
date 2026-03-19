namespace Notification.Infrastructure.Helpers
{
    public static class EmailTemplateHelper
    {
        public static string GetEnrollmentSubject(string courseTitle)
        {
            return $"Welcome to {courseTitle}!";
        }

        public static string GetCompletionSubject(string title)
        {
            return $"You completed {title}!";
        }

        public static string GetEnrollmentHtmlBody(
            string studentName,
            string courseTitle,
            string courseUrl
        )
        {
            return $@"
                    <html>
                      <body
                        style='
                          font-family: Arial, sans-serif;
                          color: #333;
                          line-height: 1.6;
                          margin: 0;
                          padding: 0;
                          background-color: #f9f9f9;
                        '
                      >
                        <table
                          align='center'
                          width='100%'
                          cellpadding='0'
                          cellspacing='0'
                          style='
                            max-width: 600px;
                            background-color: #ffffff;
                            border-radius: 8px;
                            padding: 20px;
                          '
                        >
                          <tr>
                            <td>
                              <h2 style='color: #2454b6; text-align: center'>
                                Welcome to {courseTitle}!
                              </h2>
                              <p>Hi {studentName},</p>
                              <p>
                                Congratulations, you've just taken a huge step by committing to
                                mastering a new skill with <strong>{courseTitle}</strong>. We're
                                excited for you!
                              </p>
                              <p>Here are a few tips to help you succeed:</p>
                              <ol>
                                <li>
                                  <strong>Start with the first lesson:</strong> Access your course
                                  content and begin your learning journey at your own pace. Click
                                  below to start now.
                                </li>
                                <li>
                                  <strong>Set a schedule:</strong> Plan a consistent time each week
                                  to study and progress steadily.
                                </li>
                                <li>
                                  <strong>Engage with others:</strong> Join discussions, ask
                                  questions, and connect with your peers to enhance your learning
                                  experience.
                                </li>
                              </ol>
                              <div style='text-align: center; margin: 20px 0'>
                                <a
                                  href='{courseUrl}'
                                  style='
                                    background-color: #2454b6;
                                    color: white;
                                    text-decoration: none;
                                    padding: 12px 24px;
                                    border-radius: 4px;
                                    font-weight: bold;
                                    display: inline-block;
                                  '
                                  >Start Learning Now</a
                                >
                              </div>
                              <p>We wish you a fulfilling and enjoyable learning journey ahead!</p>
                              <p>Happy Learning,<br /><strong>Stemify Team</strong></p>
                            </td>
                          </tr>
                        </table>
                      </body>
                    </html>";
        }

        public static string GetCompletionHtmlBody(string studentName, string title)
        {
            return $@"
                    <html>
                      <body
                        style='
                          font-family: Arial, sans-serif;
                          color: #333;
                          line-height: 1.6;
                          margin: 0;
                          padding: 0;
                          background-color: #f9f9f9;
                        '
                      >
                        <table
                          align='center'
                          width='100%'
                          cellpadding='0'
                          cellspacing='0'
                          style='
                            max-width: 600px;
                            background-color: #ffffff;
                            border-radius: 8px;
                            padding: 20px;
                          '
                        >
                          <tr>
                            <td>
                              <h2 style='color: #2454b6; text-align: center'>
                                {studentName}, congratulations!
                              </h2>
                              <p>Hi {studentName},</p>
                              <p>
                                Completing an online course is no simple endeavor.
                                It requires time, dedication, and commitment, so when we say ""Congratulations"" - we mean it!
                                Take a moment to reflect on your hard work and enjoy your completion of {title}. You've earned it.
                              </p>
                              <p>Once again, congratulations on your achievement.</p>
                              <p>Keep it Up,<br /><strong>Stemify Team</strong></p>
                            </td>
                          </tr>
                        </table>
                      </body>
                    </html>";
        }

        public static string GetSubscriptionExpirySubject(string organizationName, int daysUntilExpiry)
        {
            if (daysUntilExpiry <= 7)
            {
                return $"Urgent: {organizationName} Subscription Expires in {daysUntilExpiry} Days";
            }
            return $"{organizationName} Subscription Expiring Soon";
        }

        public static string GetSubscriptionExpiryHtmlBody(
            string organizationName,
            string planName,
            DateTime expiryDate,
            int daysUntilExpiry,
            int maxStudentSeats,
            int maxTeacherSeats,
            string renewalLink)
        {
            var urgencyColor = daysUntilExpiry <= 7 ? "#d9534f" : "#f0ad4e";
            var urgencyLabel = daysUntilExpiry <= 7 ? "Urgent" : "Important";

            return $@"
                <html>
                  <body
                    style='
                      font-family: Arial, sans-serif;
                      color: #333;
                      line-height: 1.6;
                      margin: 0;
                      padding: 0;
                      background-color: #f9f9f9;
                    '
                  >
                    <table
                      align='center'
                      width='100%'
                      cellpadding='0'
                      cellspacing='0'
                      style='
                        max-width: 600px;
                        background-color: #ffffff;
                        border-radius: 8px;
                        overflow: hidden;
                      '
                    >
                      <!-- Header -->
                      <tr>
                        <td style='background-color: {urgencyColor}; padding: 15px; text-align: center'>
                          <h3 style='color: white; margin: 0'>{urgencyLabel}: Subscription Expiring</h3>
                        </td>
                      </tr>

                      <!-- Content -->
                      <tr>
                        <td style='padding: 30px 20px'>
                          <h2 style='color: #2454b6; text-align: center; margin-top: 0'>
                            Your Subscription is Expiring Soon
                          </h2>

                          <p>Dear Organization Admin,</p>

                          <p>
                            This is a reminder that your <strong>Stemify</strong> subscription for
                            <strong>{organizationName}</strong> is set to expire in <strong style='color: {urgencyColor}'>{daysUntilExpiry} days</strong>.
                          </p>

                          <!-- Subscription Details Box -->
                          <table
                            width='100%'
                            cellpadding='10'
                            cellspacing='0'
                            style='
                              background-color: #f5f5f5;
                              border-radius: 6px;
                              margin: 20px 0;
                              border: 1px solid #e0e0e0;
                            '
                          >
                            <tr>
                              <td>
                                <p style='margin: 5px 0'><strong>Organization:</strong> {organizationName}</p>
                                <p style='margin: 5px 0'><strong>Current Plan:</strong> {planName}</p>
                                <p style='margin: 5px 0'><strong>Expiry Date:</strong> {expiryDate:MMMM dd, yyyy}</p>
                                <p style='margin: 5px 0'><strong>Student Seats:</strong> {maxStudentSeats}</p>
                                <p style='margin: 5px 0'><strong>Teacher Seats:</strong> {maxTeacherSeats}</p>
                              </td>
                            </tr>
                          </table>

                          <p>
                            To avoid any interruption to your service and ensure continued access to our
                            comprehensive STEM education platform for your students and teachers, please
                            renew your subscription before the expiry date.
                          </p>

                          <h3 style='color: #2454b6'>What happens if the subscription expires?</h3>
                          <ul>
                            <li>Access to courses and learning materials will be restricted</li>
                            <li>Students will no longer be able to view content or submit assignments</li>
                            <li>Teachers will lose access to classroom management tools</li>
                            <li>Progress data will be preserved but inaccessible until renewal</li>
                          </ul>

                          <!-- Renewal Button -->
                          <div style='text-align: center; margin: 30px 0'>
                            <a
                              href='{renewalLink}'
                              style='
                                background-color: #2454b6;
                                color: white;
                                text-decoration: none;
                                padding: 15px 30px;
                                border-radius: 6px;
                                font-weight: bold;
                                font-size: 16px;
                                display: inline-block;
                              '
                            >Renew Subscription Now</a>
                          </div>

                          <p>
                            If you have any questions or need assistance with the renewal process,
                            please don't hesitate to contact our support team.
                          </p>

                          <p>Thank you for choosing Stemify for your STEM education needs.</p>

                          <p style='margin-top: 30px'>
                            Best regards,<br />
                            <strong>Stemify Team</strong>
                          </p>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #666'>
                          <p style='margin: 0'>
                            This is an automated reminder. Please do not reply to this email.
                          </p>
                          <p style='margin: 5px 0'>
                            © {DateTime.UtcNow.Year} Stemify. All rights reserved.
                          </p>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>";
        }
    }
}
