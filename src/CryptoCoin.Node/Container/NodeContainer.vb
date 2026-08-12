Imports Castle.Windsor
Imports Castle.MicroKernel.Registration
Imports Castle.MicroKernel.SubSystems.Configuration
Imports CryptoCoin.Core
Imports CryptoCoin.Transactions
Imports CryptoCoin.Mining
Imports CryptoCoin.Explorer
Imports CryptoCoin.Node.Logging

Namespace CryptoCoin.Node.Container

    ''' <summary>
    ''' Castle.Windsor IoC container installer for the CryptoCoin Node.
    ''' Registers all node services and their dependencies.
    '''
    ''' Modernisation note: on .NET 10 this would be replaced by
    ''' Microsoft.Extensions.DependencyInjection with IServiceCollection.
    ''' Windsor's fluent registration API maps closely to AddSingleton/AddTransient.
    ''' </summary>
    Public Class NodeInstaller
        Implements IWindsorInstaller

        Private ReadOnly _config As NodeConfig
        Private ReadOnly _params As ChainParameters
        Private ReadOnly _blockchain As Blockchain

        Public Sub New(config As NodeConfig, params As ChainParameters, blockchain As Blockchain)
            _config = config
            _params = params
            _blockchain = blockchain
        End Sub

        Public Sub Install(container As IWindsorContainer,
                           store As IConfigurationStore) Implements IWindsorInstaller.Install

            ' Register configuration as singleton
            container.Register(
                Component.For(Of NodeConfig)() _
                         .Instance(_config) _
                         .LifestyleSingleton())

            ' Register chain parameters
            container.Register(
                Component.For(Of ChainParameters)() _
                         .Instance(_params) _
                         .LifestyleSingleton())

            ' Register blockchain (already created, register the instance)
            container.Register(
                Component.For(Of Blockchain)() _
                         .Instance(_blockchain) _
                         .LifestyleSingleton())

            ' Register mempool
            container.Register(
                Component.For(Of Mempool)() _
                         .ImplementedBy(Of Mempool)() _
                         .LifestyleSingleton())

            ' Register block assembler
            container.Register(
                Component.For(Of BlockAssembler)() _
                         .ImplementedBy(Of BlockAssembler)() _
                         .LifestyleSingleton())

            ' Register miner
            container.Register(
                Component.For(Of Miner)() _
                         .ImplementedBy(Of Miner)() _
                         .LifestyleSingleton())

            NodeLogger.Debug("[Container] Node services registered with Castle.Windsor")
        End Sub

    End Class

    ''' <summary>
    ''' Factory that creates and configures the Windsor container for the node.
    ''' </summary>
    Public Class NodeContainerFactory

        ''' <summary>
        ''' Creates a configured Windsor container with all node services registered.
        ''' </summary>
        Public Shared Function Create(config As NodeConfig,
                                      params As ChainParameters,
                                      blockchain As Blockchain) As IWindsorContainer
            Dim container As New WindsorContainer()
            container.Install(New NodeInstaller(config, params, blockchain))
            NodeLogger.Info("[Container] Castle.Windsor container initialised")
            Return container
        End Function

    End Class

End Namespace
