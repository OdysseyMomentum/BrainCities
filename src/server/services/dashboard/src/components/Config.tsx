import React, { useContext, useState } from 'react';
import { Drawer, Button, Slider, Typography, notification } from 'antd';
import { Input, Card } from 'antd';
import { GlobalContext } from '../context/GlobalContextProvider';
import { SMART_LIGHT_IDS } from '../types';
import QrReader from 'react-qr-reader';
import { showNotification } from '../helpers/notification';
import { QrcodeOutlined } from '@ant-design/icons';
import { DWCheckPerms, DWGetFile } from '../helpers';

const { Text } = Typography;

export const Config = () => {
    const context = useContext(GlobalContext);
    const config = context.config;

    const [drawerVisible, setDrawerVisible] = useState(false);
    const showDrawer = () => {
        setDrawerVisible(true);
    };
    const onClose = () => {
        setDrawerVisible(false);
    };

    const [qrSensorId, setQrSensorId] = useState<SMART_LIGHT_IDS | undefined>();

    const checkPerms = async (id?: SMART_LIGHT_IDS) => {
        try {
            const publicKey = config.smartLights[id]?.publicKey;
            const hasPerms = await DWCheckPerms(config, publicKey);
            if (hasPerms)
                return showNotification(
                    'success',
                    'Success!',
                    'All permissions needed by the operator is already granted.',
                );
            else throw new Error('Permission does not exist.');
        } catch (err) {
            console.error(err);
            return showNotification('error', 'Error!', err.message);
        }
    };

    return (
        <>
            <Button
                onClick={showDrawer}
                type="primary"
                style={{ position: 'fixed', bottom: '10px', right: '10px', zIndex: 100 }}
            >
                Show Config
            </Button>
            <Drawer title="Config" placement="right" width={'30vw'} onClose={onClose} visible={drawerVisible}>
                <Card title="Operator Credentials" style={{ marginBottom: '1.5rem' }}>
                    <Text type="secondary">Public Key</Text>
                    <Input
                        placeholder="Enter Public Key"
                        style={{ marginBottom: '1rem' }}
                        value={config.operatorDwPublicKey}
                        onChange={e => context.changeConfig({ operatorDwPublicKey: e.target.value })}
                    />
                    <Text type="secondary">Private Key</Text>
                    <Input
                        type="password"
                        placeholder="Enter Private Key"
                        value={config.operatorDwPrivateKey}
                        onChange={e => context.changeConfig({ operatorDwPrivateKey: e.target.value })}
                    />
                </Card>
                {[SMART_LIGHT_IDS.SL1, SMART_LIGHT_IDS.SL2, SMART_LIGHT_IDS.SL3].map((id: SMART_LIGHT_IDS) => {
                    return (
                        <div key={id}>
                            {qrSensorId && qrSensorId === id && (
                                <QrReader
                                    onError={console.log}
                                    onScan={value => {
                                        if (value) {
                                            context.changeConfig({
                                                smartLights: {
                                                    ...config.smartLights,
                                                    [qrSensorId]: {
                                                        ...config.smartLights[qrSensorId],
                                                        publicKey: value,
                                                    },
                                                },
                                            });
                                            setQrSensorId(undefined);
                                            showNotification(
                                                'success',
                                                'Success!',
                                                `Smart Light Public Key scanned successfully! (Scanned value: ${value})`,
                                            );
                                        }
                                    }}
                                />
                            )}
                            <Card
                                title={id}
                                style={{ marginBottom: '1.5rem' }}
                                extra={
                                    <Button type="primary" size="small" onClick={() => checkPerms(id)}>
                                        Check Perms
                                    </Button>
                                }
                            >
                                <Text type="secondary">Public Key</Text>
                                <Input
                                    suffix={<QrcodeOutlined onClick={() => setQrSensorId(id)} />}
                                    placeholder="Enter Public Key"
                                    style={{ marginBottom: '1rem' }}
                                    onChange={e =>
                                        context.changeConfig({
                                            smartLights: {
                                                ...config?.smartLights,
                                                [id]: {
                                                    ...config.smartLights[id],
                                                    publicKey: e.target.value,
                                                },
                                            },
                                        })
                                    }
                                    value={config.smartLights && config.smartLights[id]?.publicKey}
                                />
                                <Text type="secondary">Private Key</Text>
                                <Input
                                    type="password"
                                    placeholder="Enter Private Key"
                                    style={{ marginBottom: '1rem' }}
                                    onChange={e =>
                                        context.changeConfig({
                                            smartLights: {
                                                ...config?.smartLights,
                                                [id]: {
                                                    ...config.smartLights[id],
                                                    privateKey: e.target.value,
                                                },
                                            },
                                        })
                                    }
                                    value={config.smartLights && config.smartLights[id]?.privateKey}
                                />
                                <Text type="secondary">X Coordinate</Text>
                                <Slider
                                    min={1}
                                    max={100}
                                    onChange={e =>
                                        context.changeConfig({
                                            smartLights: {
                                                ...config.smartLights,
                                                [id]: {
                                                    ...config.smartLights[id],
                                                    xValue: e,
                                                },
                                            },
                                        })
                                    }
                                    value={config.smartLights && config.smartLights[id]?.xValue}
                                />
                                <Text type="secondary">Y Coordinate</Text>
                                <Slider
                                    min={1}
                                    max={100}
                                    onChange={e =>
                                        context.changeConfig({
                                            smartLights: {
                                                ...config.smartLights,
                                                [id]: {
                                                    ...config.smartLights[id],
                                                    yValue: e,
                                                },
                                            },
                                        })
                                    }
                                    value={config.smartLights && config.smartLights[id]?.yValue}
                                />
                            </Card>
                        </div>
                    );
                })}
            </Drawer>
        </>
    );
};
