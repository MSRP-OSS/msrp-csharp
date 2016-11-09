//
// Copyright © Massxess BV This file is part of MSRP-CSharp.net Stack.
// 
// MSRP-CSharp.net Stack is free software: you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as published by the
// Free Software Foundation, version 3 or later.
// 
// MSRP-CSharp.net Stack is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
// for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with MSRP-CSharp.net Stack. If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSRP
{
    /// <summary>
    /// Encapsulated knowledge on MSRP response codes.
    /// </summary>
    public class ResponseCode
    {
        internal class Code
        {
            public ResponseCodes RCCode;
            public string Description;

            public Code(ResponseCodes code, string description)
            {
                RCCode = code;
                Description = description;
            }
        }

        private static Code[] rcSet = {
		    new Code(ResponseCodes.RC200, "200 Ok, successful transaction"),
		    new Code(ResponseCodes.RC400, "400 Request unintelligible"),
		    new Code(ResponseCodes.RC403, "403 Not allowed"),
            new Code(ResponseCodes.RC404, "404 Failure to resolve recipient's URI"),
		    new Code(ResponseCodes.RC408, "408 Downstream transaction timeout"),
		    new Code(ResponseCodes.RC413, "413 Stop sending immediately"),
		    new Code(ResponseCodes.RC415, "415 Media type not supported"),
		    new Code(ResponseCodes.RC423, "423 Parameter out of bounds"),
            new Code(ResponseCodes.RC424, "424 Malformed nickname"),
		    new Code(ResponseCodes.RC425, "425 Nickname reserved or already in use"),
		    new Code(ResponseCodes.RC428, "428 Private messages not supported"),
		    new Code(ResponseCodes.RC481, "481 Session not found"),
		    new Code(ResponseCodes.RC501, "501 Unknown request"),
		    new Code(ResponseCodes.RC506, "506 Wrong session") };

        private static ResponseCodes[] _abortCode = { ResponseCodes.RC400, ResponseCodes.RC403, ResponseCodes.RC413, ResponseCodes.RC415, ResponseCodes.RC481 };

        /// <summary>
        /// Does given response code denote an error?
        /// </summary>
        /// <param name="code">the response code to check</param>
        /// <returns>true is (known) error</returns>
        public static bool IsError(ResponseCodes code)
        {
            return (int)code > 299;
        }

        public static string ToString(ResponseCodes code) 
        {
		    foreach (Code rc in rcSet) 
            {
                if (code == rc.RCCode) { return rc.Description; }
		    }
		
            return "Unknown (non-MSRP) response code";
	    }

        /// <summary>
        /// Is response code an indication to abort sending?
        /// </summary>
        /// <param name="code">the response code</param>
        /// <returns>true if the code indicates sender should abort sending.</returns>
        public static bool IsAbortCode(ResponseCodes code) 
        {
            foreach (ResponseCodes ac in _abortCode) 
            {
			    if (ac == code) { return true; }
		    }
		    
            return false;
	    }
    }

    /// <summary>
    /// MSRPResponseCodes enum
    /// </summary>
    public enum ResponseCodes
    {
        /// <summary>
        /// 
        /// </summary>
        RC200 = 200,

        /// <summary>
        /// 
        /// </summary>
        RC400 = 400,

        /// <summary>
        /// 
        /// </summary>
        RC403 = 403,

        /// <summary>
        /// 
        /// </summary>
        RC404 = 404,

        /// <summary>
        /// 
        /// </summary>
        RC408 = 408,

        /// <summary>
        /// 
        /// </summary>
        RC413 = 413,

        /// <summary>
        /// 
        /// </summary>
        RC415 = 415,

        /// <summary>
        /// 
        /// </summary>
        RC423 = 423,
        
        /// <summary>
        /// 
        /// </summary>
        RC424 = 424,

        /// <summary>
        /// 
        /// </summary>
        RC425 = 425,

        /// <summary>
        /// 
        /// </summary>
        RC428 = 428,

        /// <summary>
        /// 
        /// </summary>
        RC481 = 481,

        /// <summary>
        /// 
        /// </summary>
        RC501 = 501,

        /// <summary>
        /// 
        /// </summary>
        RC506 = 506
    }
}
