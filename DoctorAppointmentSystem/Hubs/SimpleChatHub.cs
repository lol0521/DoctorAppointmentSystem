// Hubs/SimpleChatHub.cs
using Microsoft.AspNetCore.SignalR;

namespace DoctorAppointmentSystem.Hubs
{
    public class SimpleChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            // Broadcast to all connected clients
            await Clients.All.SendAsync("ReceiveMessage", user, message, DateTime.Now);
        }

        public async Task SendBotResponse(string user, string message, bool isQuickResponse)
        {
            string lowerMessage = message.ToLower();
            string botResponse = "";

            // Handle yes/no responses to suggestion question (only if not a quick response)
            if (!isQuickResponse)
            {
                if (lowerMessage.Contains("yes") || lowerMessage.Contains("yeah") ||
                    lowerMessage.Contains("sure") || lowerMessage.Contains("ok") ||
                    lowerMessage.Contains("please") || lowerMessage.Contains("show"))
                {
                    botResponse = "Great! I've added some quick suggestion buttons below. You can click any of them or type your own question.";
                }
                else if (lowerMessage.Contains("no") || lowerMessage.Contains("not") ||
                         lowerMessage.Contains("dont") || lowerMessage.Contains("don't"))
                {
                    botResponse = "No problem! Feel free to type your question anytime.";
                    await Clients.Caller.SendAsync("ReceiveMessage", "Chat Bot", botResponse, DateTime.Now);
                }
            }

            // Medical assistance
            if (lowerMessage.Contains("emergency") || lowerMessage.Contains("urgent") ||
                lowerMessage.Contains("help") || lowerMessage.Contains("pain") ||
                lowerMessage.Contains("hurt") || lowerMessage.Contains("bleeding") ||
                lowerMessage.Contains("emergency help"))
            {
                botResponse = "For medical emergencies, please call 999 immediately or go to your nearest emergency room. Your health is our top priority.";
            }
            // Appointments
            else if (lowerMessage.Contains("appointment") || lowerMessage.Contains("book") ||
                     lowerMessage.Contains("schedule") || lowerMessage.Contains("reschedule") ||
                     lowerMessage.Contains("cancel") || lowerMessage.Contains("book an appointment"))
            {
                botResponse = "To book, reschedule, or cancel an appointment, please visit our booking page. You can also call our reception at (555) 123-4567 during business hours (9 AM to 5 PM, Monday to Friday).";
            }
            // Doctor information
            else if (lowerMessage.Contains("doctor") || lowerMessage.Contains("physician") ||
                     lowerMessage.Contains("specialist") || lowerMessage.Contains("dr.") ||
                     lowerMessage.Contains("surgeon") || lowerMessage.Contains("cardiologist") ||
                     lowerMessage.Contains("dentist") || lowerMessage.Contains("dermatologist") ||
                     lowerMessage.Contains("pediatrician") || lowerMessage.Contains("gynecologist") ||
                     lowerMessage.Contains("doctor availability"))
            {
                botResponse = "Our doctors are available from 9 AM to 5 PM daily. We have specialists in various fields including cardiology, dermatology, pediatrics, and more. You can view all our doctors and their specialties on our Doctors page.";
            }
            // Location and hours
            else if (lowerMessage.Contains("location") || lowerMessage.Contains("address") ||
                     lowerMessage.Contains("where") || lowerMessage.Contains("directions") ||
                     lowerMessage.Contains("hours") || lowerMessage.Contains("open") ||
                     lowerMessage.Contains("close") || lowerMessage.Contains("weekend") ||
                     lowerMessage.Contains("location & hours") || lowerMessage.Contains("location and hours"))
            {
                botResponse = "We're located at 123 Medical Center Drive, Health City, HC 12345. Our hours are Monday to Friday 8:30 AM to 5:30 PM, and Saturday 9:00 AM to 1:00 PM. We're closed on Sundays and major holidays.";
            }
            // Prescription related
            else if (lowerMessage.Contains("prescription") || lowerMessage.Contains("refill") ||
                     lowerMessage.Contains("medication") || lowerMessage.Contains("medicine") ||
                     lowerMessage.Contains("pharmacy") || lowerMessage.Contains("drug") ||
                     lowerMessage.Contains("prescription refill"))
            {
                botResponse = "For prescription refills, please contact your pharmacy directly. They will send us a refill request if needed. For new prescriptions or changes to existing medications, please schedule an appointment with your doctor.";
            }
            // Insurance and billing
            else if (lowerMessage.Contains("insurance") || lowerMessage.Contains("billing") ||
                     lowerMessage.Contains("payment") || lowerMessage.Contains("bill") ||
                     lowerMessage.Contains("claim") || lowerMessage.Contains("coverage") ||
                     lowerMessage.Contains("cost") || lowerMessage.Contains("price") ||
                     lowerMessage.Contains("billing question"))
            {
                botResponse = "For insurance and billing questions, please contact our billing department at (555) 123-BILL during business hours. We accept most major insurance plans. You can also view and pay your bill online through our patient portal.";
            }
            // Test results
            else if (lowerMessage.Contains("result") || lowerMessage.Contains("test") ||
                     lowerMessage.Contains("lab") || lowerMessage.Contains("x-ray") ||
                     lowerMessage.Contains("mri") || lowerMessage.Contains("scan") ||
                     lowerMessage.Contains("blood") || lowerMessage.Contains("urine") ||
                     lowerMessage.Contains("test results"))
            {
                botResponse = "Test results are typically available within 3-5 business days. Your doctor will contact you to discuss significant results. You can also view most results through our patient portal once they've been reviewed by your physician.";
            }
            // General greeting
            else if (lowerMessage.Contains("hello") || lowerMessage.Contains("hi") ||
                     lowerMessage.Contains("hey") || lowerMessage.Contains("good morning") ||
                     lowerMessage.Contains("good afternoon") || lowerMessage.Contains("good evening"))
            {
                botResponse = "Hello! How can I assist you today?";
            }
            // Thank you
            else if (lowerMessage.Contains("thank") || lowerMessage.Contains("thanks") ||
                     lowerMessage.Contains("appreciate") || lowerMessage.Contains("grateful"))
            {
                botResponse = "You're welcome! I'm happy to help. Is there anything else you'd like to know?";
            }
            // Contact information
            else if (lowerMessage.Contains("contact") || lowerMessage.Contains("phone") ||
                     lowerMessage.Contains("call") || lowerMessage.Contains("email") ||
                     lowerMessage.Contains("number") || lowerMessage.Contains("reach") ||
                     lowerMessage.Contains("contact information"))
            {
                botResponse = "You can reach us at (555) 123-4567 during business hours (9 AM to 5 PM, Monday to Friday). For after-hours emergencies, please call 999. Our email address is info@doctorappointmentsystem.com.";
            }
            // Patient portal
            else if (lowerMessage.Contains("portal") || lowerMessage.Contains("login") ||
                     lowerMessage.Contains("account") || lowerMessage.Contains("online") ||
                     lowerMessage.Contains("website") || lowerMessage.Contains("sign in"))
            {
                botResponse = "You can access our patient portal at portal.doctorappointmentsystem.com. There you can view your medical records, schedule appointments, message your doctor, and pay bills. If you need help with your account, please call our support line at (555) 123-HELP.";
            }
            // COVID related
            else if (lowerMessage.Contains("covid") || lowerMessage.Contains("coronavirus") ||
                     lowerMessage.Contains("vaccine") || lowerMessage.Contains("vaccination") ||
                     lowerMessage.Contains("mask") || lowerMessage.Contains("pandemic"))
            {
                botResponse = "We offer COVID-19 testing and vaccinations by appointment. Please call our COVID hotline at (555) 123-COVID for the most current information on our protocols and availability. Mask wearing is currently optional but recommended for those with symptoms.";
            }
            // General inquiry fallback
            else if (string.IsNullOrEmpty(botResponse))
            {
                botResponse = "Thank you for your message. I understand you're asking about: \"" + message + "\". Our team will respond to more complex inquiries shortly. In the meantime, is there anything specific I can help you with regarding appointments, doctors, prescriptions, or other services?";
            }

            await Clients.Caller.SendAsync("ReceiveMessage", "Chat Bot", botResponse, DateTime.Now);
        }
    }
}

