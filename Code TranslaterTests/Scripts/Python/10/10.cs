
float smtp_port = 587; // Standard secure SMTP port
string smtp_server = "smtp.gmail.com"; // Google SMTP Server

// Set up the email lists
string email_from = "xyz@gmail.com";
List<Object> email_list = new List<object>() { "xx@verizon.net" };
string pswd = "password";
string subject = "email test";
static void send_emails(object email_list)
{
	
	foreach (var person in email_list)
	{
		
		// Make the body of the email
		var body = "\r\n        This is an email test \r\n        Kevin\r\n        \r\n        ";
		
		// make a MIME object to define parts of the email
		var msg = MIMEMultipart();
		msg["From"] = email_from;
		msg["To"] = person;
		msg["Subject"] = subject;
		
		// Attach the body of the message
		msg.attach(MIMEText(body, "plain"));
		
		// Define the file to attach
		var file_list = new List<object>() { "xxx.docx", "yyy.docx" };
		
		// Open the file in python as a binary
		foreach (var filename in file_list)
		{
			var attachment = open(filename, "rb"); // r for read and b for binary
			
			// Encode as base 64
			var attachment_package = MIMEBase("application", "octet-stream");
			attachment_package.set_payload((attachment).read());
			encoders.encode_base64(attachment_package);
			attachment_package.add_header("Content-Disposition", "attachment; filename= " + filename);
			msg.attach(attachment_package);
		}
		// Cast as string
		var text = msg.as_string();
		
		// Connect with the server
		Console.WriteLine("Connecting to server...");
		var kjm_server = smtplib.SMTP(smtp_server, smtp_port);
		kjm_server.starttls();
		kjm_server.login(email_from, pswd);
		Console.WriteLine("Succesfully connected to server");
		Console.WriteLine();
		
		
		// Send emails to "person" as list is iterated
		Console.WriteLine($"Sending email to: {person}...");
		kjm_server.sendmail(email_from, person, text);
		Console.WriteLine($"Email sent to: {person}");
		Console.WriteLine();
	}
	
	// Close the port
	kjm_server.quit();
}


// Run the function
send_emails(email_list);