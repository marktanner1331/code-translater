import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from email.mime.base import MIMEBase
from email import encoders

smtp_port = 587                 # Standard secure SMTP port
smtp_server = "smtp.gmail.com"  # Google SMTP Server

# Set up the email lists
email_from = "xyz@gmail.com"
email_list = ["xx@verizon.net"]
pswd = 'password'
subject = 'email test'
def send_emails(email_list):

    for person in email_list:

        # Make the body of the email
        body = """
        This is an email test 
        Kevin
        
        """

        # make a MIME object to define parts of the email
        msg = MIMEMultipart()
        msg['From'] = email_from
        msg['To'] = person
        msg['Subject'] = subject

        # Attach the body of the message
        msg.attach(MIMEText(body, 'plain'))

        # Define the file to attach
        file_list = ["xxx.docx","yyy.docx"]

        # Open the file in python as a binary
        for filename in file_list:
            attachment= open(filename, 'rb')  # r for read and b for binary

            # Encode as base 64
            attachment_package = MIMEBase('application', 'octet-stream')
            attachment_package.set_payload((attachment).read())
            encoders.encode_base64(attachment_package)
            attachment_package.add_header('Content-Disposition', "attachment; filename= " + filename)
            msg.attach(attachment_package)
        # Cast as string
        text = msg.as_string()

        # Connect with the server
        print("Connecting to server...")
        kjm_server = smtplib.SMTP(smtp_server, smtp_port)
        kjm_server.starttls()
        kjm_server.login(email_from, pswd)
        print("Succesfully connected to server")
        print()


        # Send emails to "person" as list is iterated
        print(f"Sending email to: {person}...")
        kjm_server.sendmail(email_from, person, text)
        print(f"Email sent to: {person}")
        print()

    # Close the port
    kjm_server.quit()


# Run the function
send_emails(email_list)