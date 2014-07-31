using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web;

namespace HttpPostRequestCS
{
    class HttpPostRequest
    {
        public static void usage()
        {
            Console.WriteLine("Executable_file_name [-u URL] [-d data] [-s file] [-p proxy]");
            Console.WriteLine("Available options:");
            Console.WriteLine("     -u URL          URL to post data to");
            Console.WriteLine("     -d data         Data to post");
            Console.WriteLine("     -s file         File name to save response to");
            Console.WriteLine("     -p HTTP URL     Proxy to use for post operation");
            Console.WriteLine();
        }

        public static StringBuilder ValidatePostData(string postData)
        {
            StringBuilder encodedPostData = new StringBuilder();
            // These chars should be more...e.g. custom
            char[] reservedChars = { '?', '=', '&' };
            int pos, offset;

            // Validate the data to be posted
            Console.WriteLine("Validating the data to be posted...");
            offset = 0;
            while (offset < postData.Length)
            {
                pos = postData.IndexOfAny(reservedChars, offset);
                if (pos == -1)
                {
                    // Append the remaining part of the string
                    Console.WriteLine("Appending the remaining part of the string...");
                    encodedPostData.Append(HttpUtility.UrlEncode(postData.Substring(offset, postData.Length - offset)));
                    break;
                }
                // Found a special character so append up to the special character
                Console.WriteLine("Found a special character so append up to the special character...");
                encodedPostData.Append(HttpUtility.UrlEncode(postData.Substring(offset, pos - offset)));
                encodedPostData.Append(postData.Substring(pos, 1));
                offset = pos + 1;
            }
            return encodedPostData;
        }


        /// <summary>
        /// This method creates an HttpWebRequest object, sets the method to "POST",
        /// and builds the data to post. Once the HttpWebRequest object is created,
        /// the request stream is obtained and the post data is sent and the
        /// request stream closed. The response is then retrieved.
        /// </summary>
        /// <param name="postUrl">URL to post data to</param>
        /// <param name="postData">Data to post</param>
        /// <param name="proxyServer">Proxy server to use</param>
        /// <param name="saveFile">Filename to save response to</param>
        public static void HttpMethodPost(string postUrl, string postData, IWebProxy proxyServer, string saveFile)
        {
            HttpWebRequest httpRequest = null;
            HttpWebResponse httpResponse = null;
            Stream httpPostStream = null;
            BinaryReader httpResponseStream = null;
            FileStream localFile = null;

            try
            {
                StringBuilder encodedPostData;
                byte[] postBytes = null;

                // Create HTTP web request
                Console.WriteLine("Creating HTTP web request...");
                httpRequest = (HttpWebRequest)WebRequest.Create(postUrl);
                // Change method from the default "GET" to "POST"
                Console.WriteLine("Changing method from the default \"GET\" to \"POST\"...");
                httpRequest.Method = "POST";
                // Posted forms need to be encoded so change the content type
                Console.WriteLine("Changing the content type...");
                httpRequest.ContentType = "application/x-www-form-urlencoded";
                // Set the proxy
                Console.WriteLine("Setting the proxy...");
                httpRequest.Proxy = proxyServer;
                // Validate and encode the data to POST
                Console.WriteLine("Validating and encode the data to POST...");
                encodedPostData = ValidatePostData(postData);

                Console.WriteLine("Encoded POST string: '{0}'", encodedPostData.ToString());

                // Retrieve a byte array representation of the data
                Console.WriteLine("Retrieving a byte array representation of the data...");
                postBytes = Encoding.UTF8.GetBytes(encodedPostData.ToString());
                // Set the content length (the number of bytes in the POST request)
                Console.WriteLine("Setting the content length - the number of bytes in the POST request...");
                httpRequest.ContentLength = postBytes.Length;
                // Retrieve the request stream so we can write the POST data
                Console.WriteLine("Retrieving the request stream so we can write the POST data...");
                httpPostStream = httpRequest.GetRequestStream();
                // Write the POST request
                Console.WriteLine("Writing the POST request...");
                httpPostStream.Write(postBytes, 0, postBytes.Length);

                httpPostStream.Close();
                httpPostStream = null;

                // Retrieve the response
                Console.WriteLine("Retrieving the response...");
                httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                // Retrieve the response stream
                Console.WriteLine("Retrieving the response stream...");
                httpResponseStream = new BinaryReader(httpResponse.GetResponseStream(), Encoding.UTF8);

                byte[] readData;

                // Open the file to save the response to
                Console.WriteLine("Opening the file to save the response to...");
                localFile = File.Open(
                    saveFile,
                    System.IO.FileMode.Create,
                    System.IO.FileAccess.Write,
                    System.IO.FileShare.None
                    );
                Console.WriteLine("Saving response to: {0}", localFile.Name);
                Console.Write("Receiving response stream until the end...\n");
                // Receive the response stream until the end
                int count = 0;
                long percent;

                while (true)
                {
                    readData = httpResponseStream.ReadBytes(4096);
                    if (readData.Length == 0)
                        break;
                    localFile.Write(readData, 0, readData.Length);
                    // Calculate the progress and display to the console
                    Console.Write("Calculating the progress and display to the console...\n");
                    count += readData.Length;
                    percent = (count * 100) / httpResponse.ContentLength;
                    Console.Write("\b\b\b");
                    Console.Write(@"{0}%", percent.ToString().PadLeft(2));
                }
                Console.WriteLine();
            }
            catch (WebException wex)
            {
                Console.WriteLine("HttpMethodPost() - Exception occurred: {0}", wex.ToString());
                httpResponse = (HttpWebResponse)wex.Response;
            }
            finally
            {
                // Close any remaining resources
                Console.Write("Closing any remaining resources...\n");
                if (httpResponse != null)
                {
                    httpResponse.Close();
                }
                if (localFile != null)
                {
                    localFile.Close();
                }
            }
        }

        /// <summary>
        /// This is the main routine that parses the command line and calls routines to
        /// issue the POST request and receive the response.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            // Example: http://search.live.com/
            //          results.aspx?q=tenouk&go=&form=QBLH
            //          http://search.msdn.microsoft.com/
            //          Default.aspx?query=web&brand=msdn&locale=en-us&refinement=
            IWebProxy proxyServer;
            string uriToPost = "http://search.live.com/",
                    proxyName = "http://flex-proxy.nhn.no",
                    postData = "results.aspx?q=tenouk&go=&form",
                    fileName = null;

            usage();

            // Parse the command line
            for (int i = 0; i < args.Length; i++)
            {
                try
                {
                    if ((args[i][0] == '-') || (args[i][0] == '/'))
                    {
                        switch (Char.ToLower(args[i][1]))
                        {
                            case 'u':
                                // URI to post to
                                uriToPost = args[++i];
                                break;
                            case 'p':
                                // Name of proxy server to use
                                proxyName = args[++i];
                                break;
                            case 'd':
                                // Retrieve all referenced images and text on the same host
                                postData = args[++i];
                                break;
                            case 's':
                                // Local save path to append to retrieved resources
                                fileName = args[++i];
                                break;
                            default:
                                usage();
                                return;
                        }
                    }
                }
                catch
                {
                    usage();
                    return;
                }
            }

            try
            {
                // Set the proxy if supplied or use the default IE static proxy
                Console.Write("Setting the proxy if supplied or use the default IE static proxy...\n");
                if (proxyName == null)
                {
                    proxyServer = WebRequest.DefaultWebProxy;
                }
                else
                {
                    proxyServer = new WebProxy(proxyName);
                }
                // Post the request and write the response to the file
                Console.Write("Posting the request and write the response to the file...\n");
                HttpMethodPost(uriToPost, postData, proxyServer, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Main() - Exception occurred: {0}", ex.Message);
            }
            return;
            Console.Read();
        }
       
    }
}
