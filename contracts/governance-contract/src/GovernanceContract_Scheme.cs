using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Governance
{
    // Contract class must inherit the base class generated from the proto file
    public partial class GovernanceContract
    {
        public override Address AddGovernanceScheme(AddGovernanceSchemeInput input)
        {
            Assert(State.Initialized.Value, "Not initialized yet.");
            Assert(Context.Sender == State.DaoContract.Value, "No permission.");
            AssertParams(input.DaoId, input.GovernanceMechanism, input.SchemeThreshold);
            var schemePair = CalculateGovernanceSchemeHashAddressPair(input.DaoId, input.GovernanceMechanism);
            Assert(State.GovernanceSchemeMap[schemePair.SchemeAddress] == null, "Scheme already exist.");
            Assert(ValidateSchemeInfo(input), "Scheme already exist or invalid threshold.");
            var scheme = new GovernanceScheme
            {
                SchemeId = schemePair.SchemeId,
                SchemeAddress = schemePair.SchemeAddress,
                DaoId = input.DaoId,
                GovernanceMechanism = input.GovernanceMechanism,
                SchemeThreshold = input.SchemeThreshold,
                GovernanceToken = input.GovernanceToken
            };
            var schemeAddress = scheme.SchemeAddress;
            State.GovernanceSchemeMap[schemeAddress] = scheme;
            var schemeAddressList = GetDaoGovernanceSchemeAddressList(input.DaoId);
            if (!schemeAddressList.Value.Contains(schemeAddress))
            {
                schemeAddressList.Value.Add(schemeAddress);
                State.DaoSchemeAddressList[input.DaoId] = schemeAddressList;
            }
            Context.Fire(new GovernanceSchemeAdded
            {
                SchemeId = scheme.SchemeId,
                SchemeAddress = schemeAddress,
                DaoId = scheme.DaoId,
                GovernanceMechanism = scheme.GovernanceMechanism,
                SchemeThreshold = scheme.SchemeThreshold,
                GovernanceToken = scheme.GovernanceToken
            });
            return schemeAddress;
        }

        public override Empty UpdateGovernanceSchemeThreshold(UpdateGovernanceSchemeThresholdInput input)
        {
            Assert(State.Initialized.Value, "Not initialized yet.");
            AssertParams(input.DaoId, input.SchemeAddress, input.SchemeThreshold);
            Assert(Context.Sender == State.DaoContract.Value, "No permission.");
            Assert(ValidatePermission(input.DaoId, input.SchemeAddress), "Scheme not found.");
            Assert(ValidateDaoSubsistStatus(input.DaoId), "DAO not subsist. ");
            var scheme = State.GovernanceSchemeMap[input.SchemeAddress];
            Assert(scheme != null, "Scheme not found.");
            Assert(ValidateBaseGovernanceSchemeThreshold(input.SchemeThreshold), "Invalid threshold.");
            scheme.SchemeThreshold = input.SchemeThreshold;
            State.GovernanceSchemeMap[input.SchemeAddress] = scheme;
            Context.Fire(new GovernanceSchemeThresholdUpdated
            {
                DaoId = scheme.DaoId,
                SchemeAddress = scheme.SchemeAddress,
                UpdateSchemeThreshold = scheme.SchemeThreshold
            });
            return new Empty();
        }

        public override Empty RemoveGovernanceScheme(RemoveGovernanceSchemeInput input)
        {
            Assert(State.Initialized.Value, "Not initialized yet.");
            Assert(Context.Sender == State.DaoContract.Value, "No permission.");
            Assert(ValidateDaoSubsistStatus(input.DaoId), "DAO not subsist.");
            AssertParams(input.DaoId, input.SchemeAddress);
            var scheme = State.GovernanceSchemeMap[input.SchemeAddress];
            Assert(scheme != null && scheme.GovernanceMechanism == GovernanceMechanism.HighCouncil,
                "Scheme not found or only support removal of the High Council mechanism.");
            State.GovernanceSchemeMap.Remove(input.SchemeAddress);
            State.DaoSchemeAddressList[input.DaoId].Value.Remove(input.SchemeAddress);
            Context.Fire(new GovernanceSchemeThresholdRemoved
            {
                DaoId = input.DaoId,
                SchemeAddress = input.SchemeAddress
            });

            return new Empty();
        }

        public override Empty SetGovernanceToken(SetGovernanceTokenInput input)
        {
            Assert(State.Initialized.Value, "Not initialized yet.");
            Assert(Context.Sender == State.DaoContract.Value, "No permission.");
            AssertParams(input.DaoId, input.GovernanceToken);
            var schemeAddressList = State.DaoSchemeAddressList[input.DaoId];
            Assert(schemeAddressList != null && schemeAddressList.Value.Count > 0,
                "The DAO does not have a governance scheme list.");
            foreach (var schemeAddress in schemeAddressList.Value)
            {
                var scheme = State.GovernanceSchemeMap[schemeAddress];
                Assert(scheme != null, "Scheme not found.");
                scheme.GovernanceToken = input.GovernanceToken;
                State.GovernanceSchemeMap[schemeAddress] = scheme;
            }

            Context.Fire(new GovernanceTokenSet
            {
                DaoId = input.DaoId,
                GovernanceToken = input.GovernanceToken
            });

            return new Empty();
        }

        #region view

        public override GovernanceScheme GetGovernanceScheme(Address input)
        {
            return State.GovernanceSchemeMap[input] ?? new GovernanceScheme();
        }

        public override AddressList GetDaoGovernanceSchemeAddressList(Hash input)
        {
            return State.DaoSchemeAddressList[input] ?? new AddressList();
        }

        public override GovernanceSchemeList GetDaoGovernanceSchemeList(Hash input)
        {
            var result = new GovernanceSchemeList();
            var schemeAddressList = State.DaoSchemeAddressList[input];
            if (schemeAddressList == null || schemeAddressList.Value.Count == 0)
            {
                return result;
            }

            foreach (var schemeAddress in schemeAddressList.Value)
            {
                var scheme = State.GovernanceSchemeMap[schemeAddress];
                if (scheme != null)
                {
                    result.Value.Add(scheme);
                }
            }

            return result;
        }

        public override Address CalculateGovernanceSchemeAddress(CalculateGovernanceSchemeAddressInput input)
        {
            return CalculateGovernanceSchemeHashAddressPair(input.DaoId, input.GovernanceMechanism)?.SchemeAddress;
        }

        #endregion
    }
}