import React, { useState, useEffect, useContext } from 'react';
import styled from 'styled-components';
import { Drawer, Button, Divider, Typography } from 'antd';
import { SMART_LIGHT_IDS } from '../types';
import { GlobalContext } from '../context/GlobalContextProvider';
import { DWGetFile } from '../helpers/datawallet';

const { Text } = Typography;

const StyledLight = styled.img<{ top?: number; left?: number }>`
    height: 100px;
    position: fixed;
    z-index: 100;
    top: ${props => props.top}vh;
    left: ${props => props.left}vw;
`;

export const Light = ({ style, id }: { style?: any; id: SMART_LIGHT_IDS }) => {
    const context = useContext(GlobalContext);
    const { config } = context;

    const [drawerVisible, setDrawerVisible] = useState(false);
    const showDrawer = () => {
        setDrawerVisible(true);
    };
    const onClose = () => {
        setDrawerVisible(false);
    };

    const JSONData = ({ id }: { id: SMART_LIGHT_IDS }) => {
        const [data, setData] = useState('');
        useEffect(() => {
            (async () => {
                const publicKey = config.smartLights[id]?.publicKey;
                const privateKey = config.smartLights[id]?.privateKey;
                if (publicKey && privateKey) {
                    const fetchedData = await DWGetFile(publicKey, privateKey);
                    setData(fetchedData);
                }
            })();
        });
        return <Text code={true}>{data}</Text>;
    };

    return (
        <>
            <StyledLight
                src="street-light.png"
                onClick={showDrawer}
                style={style}
                top={(config.smartLights && config.smartLights[id]?.yValue) || 0}
                left={(config.smartLights && config.smartLights[id]?.xValue) || 0}
            />
            <Drawer title={id} placement="right" width={'50vw'} onClose={onClose} visible={drawerVisible}>
                <JSONData id={id} />
                <Divider />
                <p>Predictions go here</p>
            </Drawer>
        </>
    );
};

export default Light;
