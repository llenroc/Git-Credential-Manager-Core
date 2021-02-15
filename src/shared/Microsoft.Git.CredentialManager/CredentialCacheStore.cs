// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager
{
    public class CredentialCacheStore : ICredentialStore
    {
        readonly IGit _git;
        readonly IHelperProcess _helper;

        public CredentialCacheStore(IGit git, IHelperProcess helper)
        {
            _git = git;
            _helper = helper;
        }

        #region ICredentialStore

        public ICredential Get(string service, string account)
        {
            var input = MakeGitCredentialsEntry(service, account);

            var result = _helper.InvokeAsync(
                _git.GitPath,
                "credential-cache get",
                input
            ).GetAwaiter().GetResult();

            if (result.ContainsKey("username") && result.ContainsKey("password"))
            {
                return new GitCredential(result["username"], result["password"]);
            }

            return null;
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            var input = MakeGitCredentialsEntry(service, account);
            input["password"] = secret;

            // per https://git-scm.com/docs/gitcredentials :
            // For a store or erase operation, the helper’s output is ignored.
            _helper.InvokeAsync(
                _git.GitPath,
                "credential-cache store",
                input
            ).GetAwaiter().GetResult();
        }

        public bool Remove(string service, string account)
        {
            var input = MakeGitCredentialsEntry(service, account);

            // per https://git-scm.com/docs/gitcredentials :
            // For a store or erase operation, the helper’s output is ignored.
            _helper.InvokeAsync(
                _git.GitPath,
                "credential-cache erase",
                input
            ).GetAwaiter().GetResult();

            // the credential cache doesn't tell us whether anything was erased
            // but we're optimistic sorts
            return true;
        }

        #endregion

        private Dictionary<string, string> MakeGitCredentialsEntry(string service, string account)
        {
            var result = new Dictionary<string, string>();

            result["url"] = service;
            result["username"] = account;

            return result;
        }
    }
}