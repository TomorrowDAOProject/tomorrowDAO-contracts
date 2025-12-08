using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace TomorrowDAO.Contracts.DAO;

public partial class DAOContractTests : DAOContractTestBase
{
   [Fact]
   public async Task UpdateMetadataTest()
   {
      await InitializeAll();
      CheckMetadata(DaoId, "www.logo.com"); 
      await DAOContractStub.UpdateMetadata.SendAsync(new UpdateMetadataInput
      {
         DaoId = DaoId,
         Metadata = new Metadata
         {
            LogoUrl = "new logo url",
            Description = "new description",
            SocialMedia = { new Dictionary<string, string> { { "cc", "dd" } } }
         }
      });
      CheckMetadata(DaoId, "new logo url"); 
   }

   private async void CheckMetadata(Hash daoId, string logoUrl)
   {
      var metadata = await DAOContractStub.GetMetadata.CallAsync(DaoId);
      metadata.LogoUrl.ShouldBe(logoUrl);
      var old = metadata.SocialMedia["aa"];
   }
}