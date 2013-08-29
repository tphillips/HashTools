using System;
using System.IO;
using System.Web.Security;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Collections;

namespace Hasher
{
	class App
	{
		
		delegate bool Tryer(string Word, string Prepend, string Append, string Value, string Method, bool Hexify);

		[STAThread]
		static void Main(string[] args)
		{
			
			Console.WriteLine("\nHasher V0.2 - Tristan Phillips\n");

			bool Create = true;
			string Value = "";
			string Prepend = "";
			string Append = "";
			string WordList = "";
			string Method = "SHA1";
			bool Hexify = false;
			bool Simple = false;
			string FilePath = "";
			bool Personalise = false;
			bool UseUTF8 = false;

			if (args.Length == 0)
			{
				Help();
				return;
			}

			try 
			{
				for (int x = 0; x< args.Length; x++)
				{
					if (args[x] == "-D"){ Create = false; }
					if (args[x] == "-v"){ Value = args[x+1]; x++; }
					if (args[x] == "-p"){ Prepend = args[x+1]; x++; }
					if (args[x] == "-a"){ Append = args[x+1]; x++; }
					if (args[x] == "-l"){ WordList = args[x+1]; x++; }
					if (args[x] == "-m"){ Method = args[x+1]; x++; }
					if (args[x] == "-h"){ Hexify = true; }
					if (args[x] == "-s"){ Simple = true; }
					if (args[x] == "-f"){ FilePath = args[x+1]; x++; }
					if (args[x] == "-P"){ Personalise = true; }
					if (args[x] == "-U"){ UseUTF8 = true; }
					if (args[x] == "-?"){ Help(); return; }
				}
			} 
			catch 
			{
				Console.WriteLine("Invalid usage.");
				Help();
				return;
			}

			if ((Value == "" && FilePath == "") || (Value != "" && FilePath != ""))
			{
				Console.WriteLine("Specify a Value with -v OR a file with -f");
				return;
			}
			Console.WriteLine("Using " + (UseUTF8 ? "UTF8" : "ASCII") + " encoding\n");
			if (!Create)
			{
				if (WordList == "")
				{
					Console.WriteLine("Specify a Word List file with -l");
					return;
				}
				Console.WriteLine("Attempting to reverse hash " + Value);
				Console.WriteLine("Using: " + Method + " with wordlist " + WordList);
				Console.WriteLine("Input is in: " + (Hexify ? "Hex" : "Base64"));
				if (Append != "") { Console.WriteLine("Appending: " + Append + " to the guess"); }
				if (Prepend != "") { Console.WriteLine("Prepending: " + Prepend + " on to the guess"); }
				Console.WriteLine();
				StreamReader oR = new StreamReader(WordList);
				string File = oR.ReadToEnd();
				oR.Close();
				string[] Words = File.Split('\n');
				if (Personalise) { Words = PrependPersonalisedGuesses(Words); }
				int Processed = 0;
				foreach (string Line in Words)
				{
					string sLine = Line.Trim();
					if (sLine != null && sLine != "")
					{			
						if (Processed % 100 == 0)
						{
							double Perc = ((double)Processed / (double)Words.Length) * 100;
							Console.Write("\x8\x8\x8\x8\x8\x8" + Perc.ToString("f") + "%");
						}
						if (TryWord(sLine, Prepend, Append, Value, Method, Hexify, Simple, UseUTF8))
						{
							return;
						}
					}
					Processed ++;
				}
				Console.WriteLine("\n");
			} 
			else 
			{
				Console.WriteLine(Method + " Hash = " + GetHash(Prepend, Value, Append, Method, Hexify, FilePath, UseUTF8));
				return;
			}

		}

		static string GetHash(string Prepend, string Line, string Append, string Method, bool Hexify, string FilePath, bool UTF8)
		{
			string sHash = "";
			byte[] buffer = UTF8 ? UTF8Encoding.UTF8.GetBytes(Prepend + Line + Append) : ASCIIEncoding.ASCII.GetBytes(Prepend + Line + Append);
			if (FilePath != "")
			{
				FileStream FS = File.Open(FilePath, FileMode.Open);
				long FileLength = FS.Length;
				Console.WriteLine(String.Format("{0} is {1} byte(s)", FilePath, FS.Length));
				buffer = new byte[FS.Length];
				FS.Read(buffer, 0, buffer.Length);
				FS.Close();
			}
			if (Method.ToUpper() == "B64")
			{
				sHash = Convert.ToBase64String(buffer);
				return sHash;
			}
			sHash = Hash(buffer, Method, Hexify);
			return sHash;
		}

		static bool MatchHash(string Prepend, string Value, string Append, string DesiredValue, string Method, bool Safe, bool UTF8)
		{
			if (GetHash(Prepend, Value, Append, Method, Safe, "", UTF8).ToUpper() == DesiredValue.ToUpper())
			{
				return true;
			}
			return false;
		}

		static string Hash(byte[] ToHash, string Method, bool Hexify)
		{
			byte[] computedHashAsByteArray;
			if(Method.ToUpper() == "SHA1")
			{
				computedHashAsByteArray= new SHA1CryptoServiceProvider().ComputeHash(ToHash);
			}
			else if(Method.ToUpper() == "MD5")
			{
				computedHashAsByteArray= new MD5CryptoServiceProvider().ComputeHash(ToHash);
			}
			else
			{
				Console.WriteLine("Unsupported hashing algorithm type");
				return "";
			}
			if (Hexify)
			{
				string sRet = "";
				foreach (byte b in computedHashAsByteArray)
				{
					sRet += b.ToString("X").ToLower();
				}
				return sRet;
			} 
			else 
			{
				return Convert.ToBase64String(computedHashAsByteArray);
			}
		}

		static void Found(string Word)
		{
			Console.WriteLine("\r\n\r\nFound: " + Word + "\r\n");
		}

		static bool TryWord(string Word, string Prepend, string Append, string Value, string Method, bool Hexify, bool Simple, bool UTF8)
		{
			// As it comes
			string Try = Word.Trim();
			if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
			Try = Try.ToLower();
			string sOrig = Try;
			// Lowercase
			if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
			// Upercase
			Try = Try.ToUpper();
			if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
			// Propercase
			Try = Try.Substring(0,1) + Try.Substring(1,Try.Length-1).ToLower();
			if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
			if (!Simple)
			{
				// Doubled up in lowercase
				Try = sOrig + sOrig;
				if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
				// Double Propercase
				Try = sOrig.Substring(0,1).ToUpper() + sOrig.Substring(1,sOrig.Length-1).ToLower();
				Try = Try + Try;
				if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
				// Word and Year
				Try = sOrig + DateTime.Now.Year;
				if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
				// Word00x
				for (int y = 0; y < 10; y++)
				{
					Try = sOrig + "00" + y.ToString();
					if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
				}
				// Word0x
				for (int y = 0; y < 10; y++)
				{
					Try = sOrig + "0" + y.ToString();
					if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
				}
				// Wordx
				for (int y = 0; y < 200; y++)
				{
					Try = sOrig + y.ToString();
					if (MatchHash(Prepend, Try, Append, Value, Method, Hexify, UTF8)) { Found(Try); return true; }
				}
			}
			return false;
		}

		static string[] PrependPersonalisedGuesses(string[] CurrentList)
		{
			string[] Personal = DoGetPersonalDetails();
			Console.WriteLine("\nGenerated the following \"personal\" guesses:\n");
			foreach (string item in Personal) { Console.Write(item.ToLower() + ", "); }
			Console.WriteLine("\n");
			string[] result = new string[Personal.Length + CurrentList.Length];
			Personal.CopyTo(result, 0);
			CurrentList.CopyTo(result, Personal.Length);
			return result;
		}

		static string[] DoGetPersonalDetails()
		{
			Console.WriteLine("Answer the following questions on behalf of the \"victim\":\n");
			int MonthOfBirth = 0;
			int	DayOfBirth = 0;
			int	YearOfBirth = 0;
			Console.Write("First Name: ");
			string FirstName = Console.ReadLine();
			Console.Write("Last Name: ");
			string LastName = Console.ReadLine();
			Console.Write("Middle Name: ");
			string MiddleName = Console.ReadLine();
			Console.Write("Nick Name: ");
			string NickName = Console.ReadLine();
			Console.Write("Month Of Birth: ");
			try { MonthOfBirth = int.Parse(Console.ReadLine()); } catch{}
			Console.Write("Day of Birth: ");
			try { DayOfBirth = int.Parse(Console.ReadLine()); } catch{}
			Console.Write("Year of Birth: ");
			try { YearOfBirth = int.Parse(Console.ReadLine()); } catch{}
			Console.Write("Home tel: ");
			string HomeTelephone = Console.ReadLine();
			Console.Write("Mobile tel: ");
			string Mobile = Console.ReadLine();
			Console.Write("House no: ");
			string HouseNumber = Console.ReadLine();
			Console.Write("Street: ");
			string Street = Console.ReadLine();
			Console.Write("Town: ");
			string Town = Console.ReadLine();
			Console.Write("City: ");
			string City = Console.ReadLine();
			Console.Write("Country: ");
			string Country = Console.ReadLine(); 
			Console.Write("Favourite Country: ");
			string HolidayCountry = Console.ReadLine(); 
			Console.Write("Pets Name: ");
			string PetsName = Console.ReadLine(); 
			Console.Write("Spouse First Name: ");
			string SpouseFirstName = Console.ReadLine();
			Console.Write("Spouse Last Name: ");
			string SpouseLastName = Console.ReadLine(); 
			Console.Write("Child First Name: ");
			string ChildFirstName = Console.ReadLine(); 
			Console.Write("Favorite Color: ");
			string Color = Console.ReadLine();
			Console.Write("Mothers Maiden Name: ");
			string MothersMaidenName = Console.ReadLine();
			return CreatePersonalGuesses(FirstName, LastName, MiddleName, NickName,
				MonthOfBirth, DayOfBirth, YearOfBirth, HomeTelephone, 
				Mobile, HouseNumber, Street, Town, City, 
				Country, HolidayCountry, PetsName, SpouseFirstName, 
				SpouseLastName, ChildFirstName, Color, MothersMaidenName);
		}

		static string[] CreatePersonalGuesses(string FirstName, string LastName, string MiddleName, 
			string NickName, int MonthOfBirth, int DayOfBirth, int YearOfBirth, string HomeTelephone, 
			string Mobile, string HouseNumber, string Street, string Town, string City, 
			string Country, string HolidayCountry, string PetsName, string SpouseFirstName, 
			string SpouseLastName, string ChildFirstName, string Color, string MothersMaidenName)
		{
			ArrayList guesses = new ArrayList();
			// Straight guesses
			guesses.Add(FirstName);
			guesses.Add(LastName);
			guesses.Add(MiddleName); 
			guesses.Add(NickName); 
			guesses.Add(MonthOfBirth.ToString()); 
			guesses.Add(DayOfBirth.ToString());
			guesses.Add(YearOfBirth.ToString());
			guesses.Add(HomeTelephone);
			guesses.Add(Mobile);
			guesses.Add(HouseNumber);  
			guesses.Add(Street); 
			guesses.Add(Town); 
			guesses.Add(City); 
			guesses.Add(Country); 
			guesses.Add(HolidayCountry); 
			guesses.Add(PetsName);
			guesses.Add(SpouseFirstName); 
			guesses.Add(SpouseLastName); 
			guesses.Add(ChildFirstName); 
			guesses.Add(Color);
			guesses.Add(MothersMaidenName);
			// Clever guesses
			guesses.Add(FirstName + LastName);
			guesses.Add(NickName + LastName);
			try { guesses.Add(NickName + LastName.Substring(0,1)); } catch{}
			try { guesses.Add(FirstName + LastName.Substring(0,1)); } catch{}
			try { guesses.Add(FirstName.Substring(0,1) + LastName); } catch{}
			try { guesses.Add(FirstName.Substring(0,1) + LastName.Substring(0,1)); } catch{}
			try { guesses.Add(FirstName.Substring(0,1) + MiddleName.Substring(0,1) + LastName.Substring(0,1)); } catch{}
			try { guesses.Add(FirstName + MiddleName.Substring(0,1) + LastName); } catch{}
			guesses.Add(FirstName + "4" + SpouseFirstName);
			guesses.Add(SpouseFirstName + "4" + FirstName);
			guesses.Add(HouseNumber + Street);
			// Add year of birth to all gueeses so far.
			ArrayList more = new ArrayList();
			foreach (string item in guesses)
			{
				more.Add(item + YearOfBirth);
			}
			foreach (string item in more) { guesses.Add(item); }
			// Return string array
			string[] ret = new string[guesses.Count];
			guesses.CopyTo(ret);
			return ret;
		}

		static void Help()
		{
			Console.WriteLine("Usage: hasher [-D | -v <value> | -p <text> | -a <text> | -l <path> | -m <method> | -h | -s | -?]\n");
			Console.WriteLine("Valid arguments are:" + "\n\n" + 
				"-D	- Attempt reverse hash using word list" + "\n" + 
				"-v	- <value> Value for hashing or reversing" + "\n" + 
				"-p	- <value> Prepend value to value for hashing" + "\n" + 
				"-a	- <value> Append value to value for hashing" + "\n" + 
				"-l	- <value> Path to wordlist file" + "\n" + 
				"-m	- <SHA1/MD5/B64> hash method (default SHA1)" + "\n" + 
				"-h	- Produce hex output rather than base 64" + "\n" + 
				"-s	- Do not try common password modification methods" + "\n" + 
				"-P	- Personalise guesses based on \"victim\" details" + "\n" + 
				"-U	- Use UTF8 encoding instead of ASCII" + "\n" +
				"-?	- Show this message" + "\n");
		}

	}
}
