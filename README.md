# TomorrowDAO-contracts

The TomorrowDAO Contract is a new and cool smart contract wallet in aelf ecosystem that allows users to create and manage their own DAOs. Within Tomorrow DAO, any user can establish their own DAO, choose between the use of FT or NFT as governance tokens, and invite members to participate in DAO governance. Users can create proposals and vote on decisions within their DAO, as well as deposit funds into the DAO's treasury to manage the DAO. Tomorrow DAO also provides a user-friendly front-end interface, making it easier for users to initiate governance proposals and execute smart contract functions.

## Installation

Before cloning the code and deploying the contract, command dependencies and development tools are needed. You can follow:

- [Common dependencies](https://aelf-boilerplate-docs.readthedocs.io/en/latest/overview/dependencies.html)
- [Building sources and development tools](https://aelf-boilerplate-docs.readthedocs.io/en/latest/overview/tools.html)

The following command will clone TomorrowDAO Contract into a folder. Please open a terminal and enter the following command:

```Bash
git clone https://github.com/TomorrowDAOProject/tomorrowDAO-contracts
```
### Test

You can easily run unit tests on TomorrowDAO Contracts. Navigate to the TomorrowDAOContracts.contracts and run:

```Bash
cd /dao-contract
dotnet test
cd ../governance-contract
dotnet test
cd ../treasury-contract
dotnet test
cd ../vote-contract
dotnet test
```

## Usage

The TomorrowDAO Contract provides the following modules:

- `dao`: Create and manage your DAO.
- `governance`: Manage the proposal of DAO.
- `treasury`: Manage the treasury of DAO.
- `vote`: Manage the vote of proposal.

To use these modules, you must first deploy the TomorrowDAO Contract on aelf blockchain. Once it's deployed, you can interact with the contract using any aelf-compatible wallet or client.

## Contributing

We welcome contributions to the TomorrowDAO Contract project. If you would like to contribute, please fork the repository and submit a pull request with your changes. Before submitting a pull request, please ensure that your code is well-tested and adheres to the aelf coding standards.

## License

TomorrowDAO Contract is licensed under [MIT](https://github.com/TomorrowDAOProject/tomorrowDAO-contracts/blob/master/README.md).

## Contact

If you have any questions or feedback, please feel free to contact us at the TomorrowDAO community channels. You can find us on Discord, Telegram, and other social media platforms.

Links:

- Website: https://tmrwdao.com/
- Twitter: https://x.com/tmrwdao
- Discord: https://discord.com/invite/gTWkeR5pQB
- Telegram: https://t.me/tmrwdao