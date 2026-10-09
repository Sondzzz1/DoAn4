import Modal from '../common/Modal';
import type { ComponentProps } from 'react';

export default function LandlordModal(props: ComponentProps<typeof Modal>) {
  return <Modal {...props} bodyClassName={`landlord-modal p-4 sm:p-6 ${props.bodyClassName || ''}`} />;
}
