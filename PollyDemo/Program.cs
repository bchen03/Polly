using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;

namespace PollyDemo
{
	class Program
	{
		static int count = 0;

		static void Main(string[] args)
		{
			//SimpleRetry();
			//WaitAndRetryException();
			//WaitAndRetryReturnResult();
			CircuitBreaker();
		}

		#region SimpleRetry
		static void SimpleRetry()
		{
			int maxRetries = 2;
			int tries = 0;

			try
			{
				var result = Policy
					.Handle<Exception>()
					.Retry(maxRetries, (ex, retryCount, context) =>
					{
						Console.WriteLine("Retry exception: " + ex.Message + "," + retryCount.ToString());
						tries = retryCount;
					})
					.Execute(() =>
					{
						if (tries == maxRetries)
							return "Done on " + maxRetries;
						else
							throw new Exception("Error");
					});

				if (result != null)
					Console.WriteLine("Result: " + result);
				else
					Console.WriteLine("Result is null");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Outer exception:" + ex.Message);
			}
		}
		#endregion

		#region WaitAndRetryException
		static void WaitAndRetryException()
		{
			int maxRetries = 5;
			int tries = 0;

			try
			{
				var result = Policy
					.Handle<Exception>()
					.WaitAndRetry(maxRetries, retry =>
					{
						double pow = Math.Pow(2, retry);
						Console.WriteLine("WaitAndRetry: " + retry.ToString() + ", " + pow.ToString()); 
						tries = retry;
						return TimeSpan.FromSeconds(pow);
					})
					.Execute(() =>
					{
						if (tries == maxRetries)
							return "Done on " + maxRetries;
						else
							throw new Exception("Error");
					});

				if (result != null)
					Console.WriteLine("Result: " + result);
				else
					Console.WriteLine("Result is null");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Outer exception:" + ex.Message);
			}
		}
		#endregion

		#region WaitAndRetryReturnResult
		static void WaitAndRetryReturnResult()
		{
			int maxRetries = 4;
			int tries = 0;

			try
			{
				var result = Policy
					.HandleResult<string>(s => s == "Error")
					.OrResult(s => s == "Exception")
					.WaitAndRetry(maxRetries, retry =>
					{
						double pow = Math.Pow(2, retry);
						Console.WriteLine("WaitAndRetryReturnResults: " + retry.ToString() + ", " + pow.ToString());
						tries = retry;
						return TimeSpan.FromSeconds(pow);
					})
					.Execute(() =>
					{
						if (tries == maxRetries - 2)
							return "Done on " + maxRetries;
						else if (tries == 1)
							return "Error";
						else
							return "Exception";
					});

				if (result != null)
					Console.WriteLine("Result: " + result);
				else
					Console.WriteLine("Result is null");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Outer exception:" + ex.Message);
			}
		}
		#endregion

		#region CircuitBreaker

		public static void CircuitBreaker()
		{
			Console.WriteLine("{0} CircuitBreaker started!!!", DateTime.Now.ToString());

			var policy = default(CircuitBreakerPolicy<string>);

			policy =
				Policy
				.HandleResult<string>(s => s == "Error")	// Policy is checking if the result == "Error"
//				.Or<BrokenCircuitException>()
				.CircuitBreaker				
				(
					5,										// If it happens n times in a row...
					TimeSpan.FromSeconds(2),				// then keep circuit broken for n seconds...
					(exception, timespan) =>				// When circuit is broken execute this action (can be used to log when circuit breaks)
					{
						Console.WriteLine("{0} Break called", DateTime.Now.ToString());
					},
					() =>									// When circuit is reset execute this action
					{
						Console.WriteLine("{0} Reset called", DateTime.Now.ToString());
					}
				);

			while (true)
			{
				string result = RunCircuitBreaker(policy);	// Now keep running the code as a Func<string> within the circuit breaker until it's done
				if (result == "Done")
				{
					Console.WriteLine("{0} We're finally done!", DateTime.Now.ToString());
					break;
				}
				else
					Console.WriteLine("{0} An error? Damn...", DateTime.Now.ToString());
			}

			Console.WriteLine("{0} CircuitBreaker done!!!", DateTime.Now.ToString());
		}

		private static string RunCircuitBreaker(CircuitBreakerPolicy<string> policy)
		{
			try
			{
				string result = policy.Execute(() =>			// Running the code...
				{
					Thread.Sleep(1000);
					count++;

					if (count < 10)
					{
						Console.WriteLine("{0} Hey! This smart code is returning an error! - count: {1}", DateTime.Now.ToString(), count);
						return "Error";
					}
					else
					{
						Console.WriteLine("{0} Hey! This smart code is done! - count: {1}", DateTime.Now.ToString(), count);
						return "Done";
					}
				});

				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.GetType().Name);	// When code is run and circuit breaker is broken, a BrokenCircuitException is thrown
				Thread.Sleep(500);
				return "Error";
			}

		}

		#endregion

	}
}
